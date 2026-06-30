using System.Collections;
using UnityEngine;

// プレイヤーのHP管理と、撃破時のリスポーンを担当する。
// プレイヤーPrefabにアタッチする（PlayerRegistrarと同居）。
public class PlayerHPManager : MonoBehaviour
{
    [SerializeField] float respawnDelay = 2f;     // 撃破からリスポーンまでの待機秒数
    [SerializeField] Transform respawnPoint;       // リスポーン位置（未設定なら原点）

    PlayerRegistrar registrar;
    PlayerStatus status;
    bool isDead; // リスポーン処理中の多重発火防止

    // Projectileから当たり判定に使う公開情報
    public int PlayerId => registrar != null ? registrar.PlayerId : -1;
    public int Team => status != null ? status.teamNumber : -1;

    void Start()
    {
        registrar = GetComponent<PlayerRegistrar>();
        status = PlayerStatusManager.Instance.GetStatus(PlayerId);
    }

    // 被弾時にProjectileから呼ばれる
    public void TakeDamage(int damage)
    {
        if (status == null || isDead) return;

        status.health -= damage;
        Debug.Log($"Player {PlayerId} がダメージ {damage} を受けた → HP {status.health}");

        if (status.health <= 0)
        {
            StartCoroutine(Die());
        }
    }

    // 撃破→待機→リスポーンの流れ
    // 注意: GameObject自体をSetActive(false)するとこのコルーチンも止まるため、
    //       表示・操作用のコンポーネントだけを個別に無効化する。
    IEnumerator Die()
    {
        isDead = true;
        Debug.Log($"Player {PlayerId} は倒れた");

        // TODO: 所持宝石(status.Crystals)のドロップ処理は今回未実装

        // プレイヤーを一時的に無効化（操作・表示・当たり判定を止める）
        SetPlayerEnabled(false);

        yield return new WaitForSeconds(respawnDelay);

        // ステータスと位置を初期化して復帰
        status.health = 100;
        Vector3 pos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        transform.position = pos;

        SetPlayerEnabled(true);
        isDead = false;
        Debug.Log($"Player {PlayerId} がリスポーンした");
    }

    // 表示・操作・当たり判定をまとめて切り替える（リスポーン演出用）
    void SetPlayerEnabled(bool enabled)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = enabled;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = enabled;

        var move = GetComponent<PlayerMoveControll>();
        if (move != null) move.enabled = enabled;
        var attack = GetComponent<PlayerAttackController>();
        if (attack != null) attack.enabled = enabled;
    }
}
