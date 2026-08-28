using UnityEngine;

// プレイヤーPrefabにアタッチ
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] GameObject projectilePrefab;   // 発射する弾のPrefab

    [SerializeField] float r = 6f;  // GridSysがないシーンで使う予備射程
    [SerializeField] int a = 10;    // 威力
    [SerializeField] float v = 12f; // 速度
    [SerializeField, Min(0f)] float rangeMultiplier = 2f;
    [SerializeField, Min(0f)] float projectileSpeedMultiplier = 3f;
    [SerializeField, Min(0)] int specialAttackCost = 3;
    [SerializeField, Min(0)] int specialAttackDamage = 30;

    [SerializeField] float fireDelay = 0.5f;    // 発射後の受付停止時間
    [SerializeField] float spawnOffset = 1f;    // 発射位置調整

    ControllerInput input;
    PlayerRegistrar registrar;
    PlayerStatus status;

    Vector3 lastDir = Vector3.forward;  // 向き
    float cooldown; // 0以下なら発射可

    public float CloseRange => BaseRange * rangeMultiplier;
    public float FarRange => CloseRange * 2f;

    float BaseRange => GridSys.Instance != null ? GridSys.Instance.CellSize : r;

    void Start()
    {
        input = GetComponent<ControllerInput>();
        registrar = GetComponent<PlayerRegistrar>();
        RefreshStatus();
    }

    void Update()
    {
        Vector2 dpad = input.Dpad;
        if (dpad.sqrMagnitude > 0.01f)
        {
            lastDir = new Vector3(dpad.x, 0f, dpad.y).normalized;
            if (registrar != null) registrar.SetFacingDirection(lastDir);
        }
        else if (registrar != null)
        {
            lastDir = registrar.FacingDirection;
        }

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (input.ButtonX)
        {
            FireSpecial(lastDir, -lastDir); // アバターの前後
        }
        else if (input.ButtonY)
        {
            Vector3 left = new Vector3(-lastDir.z, 0f, lastDir.x);
            FireSpecial(left, -left); // アバターの左右
        }
        else if (input.ButtonA)
        {
            Fire(damage: a * 2, speed: v * projectileSpeedMultiplier, range: CloseRange, direction: lastDir); // 近: 射程2マス
        }
        else if (input.ButtonB)
        {
            Fire(damage: a, speed: SpecialProjectileSpeed, range: FarRange, direction: lastDir); // 遠: 射程4マス
        }
    }

    float SpecialProjectileSpeed => v * 1.5f * projectileSpeedMultiplier;

    void FireSpecial(Vector3 firstDirection, Vector3 secondDirection)
    {
        if (projectilePrefab == null || !TrySpendCrystals(specialAttackCost)) return;

        Fire(specialAttackDamage, SpecialProjectileSpeed, FarRange, firstDirection);
        Fire(specialAttackDamage, SpecialProjectileSpeed, FarRange, secondDirection);
    }

    bool TrySpendCrystals(int cost)
    {
        RefreshStatus();
        if (status == null || status.Crystals < cost) return false;

        status.Crystals -= cost;
        return true;
    }

    void RefreshStatus()
    {
        var manager = PlayerStatusManager.Instance;
        int playerId = registrar != null ? registrar.PlayerId : -1;
        status = manager != null && playerId >= 0 && playerId < manager.Count
            ? manager.GetStatus(playerId)
            : null;
    }

    void Fire(int damage, float speed, float range, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;
        direction.Normalize();

        RefreshStatus();
        int ownerTeam = status != null ? status.teamNumber : -1;
        int ownerId = registrar != null ? registrar.PlayerId : -1;

        Vector3 spawnPos = transform.position + direction * spawnOffset;
        GameObject gobj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var proj = gobj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = damage;
            proj.speed = speed;
            proj.range = range;
            proj.ownerId = ownerId;
            proj.ownerTeam = ownerTeam;
            proj.direction = direction;
        }

        cooldown = fireDelay;
    }
}
