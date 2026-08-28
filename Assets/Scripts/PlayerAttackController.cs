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

    [SerializeField] float fireDelay = 0.5f;    // 発射後の受付停止時間
    [SerializeField] float spawnOffset = 1f;    // 発射位置調整

    ControllerInput input;
    PlayerRegistrar registrar;
    int team = -1;

    Vector3 lastDir = Vector3.forward;  // 向き
    float cooldown; // 0以下なら発射可

    public float CloseRange => BaseRange * rangeMultiplier;
    public float FarRange => CloseRange * 2f;

    float BaseRange => GridSys.Instance != null ? GridSys.Instance.CellSize : r;

    void Start()
    {
        input = GetComponent<ControllerInput>();
        registrar = GetComponent<PlayerRegistrar>();

        var status = PlayerStatusManager.Instance.GetStatus(registrar.PlayerId);
        if (status != null) team = status.teamNumber;
    }

    void Update()
    {
        Vector2 dpad = input.Dpad;
        if (dpad.sqrMagnitude > 0.01f)
        {
            lastDir = new Vector3(dpad.x, 0f, dpad.y).normalized;
        }

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (input.ButtonA)
        {
            Fire(damage: a * 2, speed: v * projectileSpeedMultiplier, range: CloseRange); // 近: 射程2マス
        }
        else if (input.ButtonB)
        {
            Fire(damage: a, speed: v * 1.5f * projectileSpeedMultiplier, range: FarRange); // 遠: 射程4マス
        }
    }

    void Fire(int damage, float speed, float range)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + lastDir * spawnOffset;
        GameObject gobj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var proj = gobj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = damage;
            proj.speed = speed;
            proj.range = range;
            proj.ownerId = registrar.PlayerId;
            proj.ownerTeam = team;
            proj.direction = lastDir;
        }

        cooldown = fireDelay;
    }
}
