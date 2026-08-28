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
    Collider projectileCollider;
    Vector3 SpeedV; // 速度ベクトル
    PlayerHPManager hp; // 被弾者のHP
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
        SpeedV = direction.normalized * speed;
        rb.linearVelocity = SpeedV;

        var grid = GridSys.Instance;
        if (grid != null && !grid.IsInsideField(rb.position, GetHorizontalHalfExtents()))
        {
            Destroy(gameObject);
            return;
        }

        var aura = GetComponent<ProjectileAura>();
        if (aura == null) aura = gameObject.AddComponent<ProjectileAura>();
        aura.Initialize(GetCurrentOwnerTeam());
        Destroy(gameObject , range / speed);
    }

    void FixedUpdate()
    {
        var grid = GridSys.Instance;
        if (grid == null || rb == null) return;

        Vector2 halfExtents = GetHorizontalHalfExtents();
        Vector3 nextPosition = rb.position + SpeedV * Time.fixedDeltaTime;
        if (!grid.IsInsideField(rb.position, halfExtents)
            || !grid.IsInsideField(nextPosition, halfExtents))
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var fieldCrystal = other.GetComponentInParent<FieldCrystal>();
        if (fieldCrystal != null)
        {
            // 宝石の外側へ少し戻した位置に生成し、フィールドオブジェクト内への埋没を防ぐ。
            Vector3 dropPosition = transform.position - direction.normalized * 0.25f;
            fieldCrystal.Hit(damage, dropPosition);
            Destroy(gameObject);
            return;
        }

        hp = other.GetComponent<PlayerHPManager>();
        if(hp == null) return;
        else if(hp.PlayerId == ownerId)return;
        // 発射時のキャッシュ値だけに頼らず、現在の所属チームでも照合する。
        // チーム確定前に生成されたPlayerAttackControllerが残っていても誤射しない。
        else if(IsFriendly(hp))return;
        else
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    bool IsFriendly(PlayerHPManager target)
    {
        if (target == null) return false;

        int currentOwnerTeam = GetCurrentOwnerTeam();
        return currentOwnerTeam >= 0
            && target.Team >= 0
            && target.Team == currentOwnerTeam;
    }

    int GetCurrentOwnerTeam()
    {
        int currentOwnerTeam = ownerTeam;
        var manager = PlayerStatusManager.Instance;
        if (manager != null && ownerId >= 0 && ownerId < manager.Count)
        {
            var ownerStatus = manager.GetStatus(ownerId);
            if (ownerStatus != null) currentOwnerTeam = ownerStatus.teamNumber;
        }

        return currentOwnerTeam;
    }

    Vector2 GetHorizontalHalfExtents()
    {
        if (projectileCollider == null) return Vector2.zero;
        Vector3 extents = projectileCollider.bounds.extents;
        return new Vector2(extents.x, extents.z);
    }
}
