using UnityEngine;

// 発射体（弾）の情報を保持する。
// ★ロジック（移動・被弾処理）は未実装のスケルトンです。
//   実装手順は「Projectile_実装ガイド.md」を参照してください。
//
// 使い方: PlayerAttackController が発射時に Instantiate し、
//         直後に下の public フィールドへ値を代入します。
//         つまり Start が走る時点では各フィールドに値が入っています。
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    // ── PlayerAttackController から発射時にセットされる情報 ──
    public int damage;        // 与えるダメージ
    public float speed;       // 飛ぶ速さ
    public float range;       // 飛距離（range / speed 秒で消滅させる）
    public int ownerId;       // 撃った人の playerId（自分には当てない）
    public int ownerTeam;     // 撃った人のチーム（味方には当てない）
    public Vector3 direction; // 飛ぶ向き（正規化済みでなくてもよい）
    Rigidbody rb;
    Vector3 SpeedV; // 速度ベクトル
    PlayerHPManager hp; // 被弾者のHP
    void Start()
    {
        GetComponent<Rigidbody>();
        SpeedV = direction.normalized * speed;
        rb.linearVelocity = SpeedV;
        Destroy(gameObject , range / speed);

        // TODO(コラボレーター): ガイドの「Startでやること」を実装
        // - Rigidbody を取得して direction * speed の速度を与える
        // - range / speed 秒後に自動で消す
    }

    void OnTriggerEnter(Collider other)
    {
        hp = GetComponent<PlayerHPManager>();
        if(hp == null) return;
        else if(hp.PlayerId == ownerId)return;
        else if(hp.Team == ownerTeam)
        {
            Destroy(gameObject); 
            return;
        }
        else
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
        }

        // TODO(コラボレーター): ガイドの「OnTriggerEnterでやること」を実装
        // - 当たった相手の PlayerHPManager を取得
        // - 自分(ownerId) / 味方(ownerTeam) / 敵 で処理を分岐
    }
}
