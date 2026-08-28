using System.Collections;
using UnityEngine;

// プレイヤーのHP管理と、撃破時のリスポーンを担当する。
// プレイヤーPrefabにアタッチする（PlayerRegistrarと同居）。
public class PlayerHPManager : MonoBehaviour
{
    [SerializeField] float respawnDelay = 2f;     // 撃破からリスポーンまでの待機秒数
    [SerializeField] Transform respawnPoint;       // チーム位置を取得できない場合の予備リスポーン位置

    PlayerRegistrar registrar;
    PlayerStatus status;
    bool isDead; // リスポーン処理中の多重発火防止

    // Projectileから当たり判定に使う公開情報
    public int PlayerId => registrar != null ? registrar.PlayerId : -1;
    public int Team => status != null ? status.teamNumber : -1;
    public bool IsDead => isDead;

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

        // 所持ポイント全量を、ポイント値を保持した1つの宝石として落とす。
        int carriedPoints = status.Crystals;
        if (carriedPoints > 0)
        {
            var spawner = FieldObjectSpawner.Instance;
            var droppedCrystal = spawner != null
                ? spawner.SpawnDroppedCrystal(transform.position, carriedPoints)
                : null;

            // 生成に失敗した場合はポイントを失わせない。
            if (droppedCrystal != null) status.Crystals = 0;
            else Debug.LogWarning($"Player {PlayerId} の所持宝石をドロップできなかった", this);
        }

        // プレイヤーを一時的に無効化（操作・表示・当たり判定を止める）
        SetPlayerEnabled(false);

        yield return new WaitForSeconds(respawnDelay);

        // ステータスと位置を初期化して復帰
        status.health = 100;
        Vector3 pos;
        var rotation = transform.rotation;
        if (GridSys.Instance == null
            || !GridSys.Instance.TryGetRespawnPose(PlayerId, transform, out pos, out rotation))
        {
            pos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        }

        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(pos, rotation);

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
