using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public int damage;  // ダメージ
    public float speed; // 飛ぶ速さ
    public float range; // 飛距離
    public int ownerId; // 撃った人のplayerId
    public int ownerTeam;   // 撃った人のチーム
    public Vector3 direction;   // 飛ぶ向き
    Rigidbody rb;
    Vector3 SpeedV; // 速度ベクトル
    PlayerHPManager hp; // 被弾者のHP
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        SpeedV = direction.normalized * speed;
        rb.linearVelocity = SpeedV;
        Destroy(gameObject , range / speed);
    }

    void OnTriggerEnter(Collider other)
    {
        hp = other.GetComponent<PlayerHPManager>();
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
    }
}
