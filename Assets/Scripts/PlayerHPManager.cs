using System.Collections;
using UnityEditor.UI;
using UnityEngine;

// プレイヤーのHP管理と、撃破時のリスポーンを担当する。
// プレイヤーPrefabにアタッチする（PlayerRegistrarと同居）。
public class PlayerHPManager : MonoBehaviour
{
    PlayerRegistrar registrar;
    PlayerStatus status;
    RespawnManager RespawnManager;
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
        bool Died = RespawnManager.isDead;
        if (status == null || Died) return;

        status.health -= damage;
        Debug.Log($"Player {PlayerId} がダメージ {damage} を受けた → HP {status.health}");

        if (status.health <= 0)
        {
            RespawnManager.Respawn(PlayerId);
        }
    }

    // 撃破→待機→リスポーンの流れ
    // 注意: GameObject自体をSetActive(false)するとこのコルーチンも止まるため、
    //       表示・操作用のコンポーネントだけを個別に無効化する。
    

    // 表示・操作・当たり判定をまとめて切り替える（リスポーン演出用）
    public void SetPlayerEnabled(bool enabled)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = enabled;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = enabled;

        var move = GetComponent<PlayerMoveControll>();
        if (move != null) move.enabled = enabled;
        var attack = GetComponent<PlayerAttackController>();
        if (attack != null) attack.enabled = enabled;
    }
}
