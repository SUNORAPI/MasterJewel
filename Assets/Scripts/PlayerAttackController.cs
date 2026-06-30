using UnityEngine;

// プレイヤーPrefabにアタッチ
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] GameObject projectilePrefab;   // 発射する弾のPrefab

    [SerializeField] float r = 6f;  // 射程
    [SerializeField] int a = 10;    // 威力
    [SerializeField] float v = 12f; // 速度

    [SerializeField] float fireDelay = 0.5f;    // 発射後の受付停止時間
    [SerializeField] float spawnOffset = 1f;    // 発射位置調整

    ControllerInput input;
    PlayerRegistrar registrar;
    int team = -1;

    Vector3 lastDir = Vector3.forward;  // 向き
    float cooldown; // 0以下なら発射可

    void Start()
    {
        input = GetComponent<ControllerInput>();
        registrar = GetComponent<PlayerRegistrar>();

        var status = PlayerStatusManager.Instance.GetStatus(registrar.PlayerId);
        if (status != null) team = status.teamNumber;
    }

    void Update()
    {
        // 入力方向を覚えておく（停止中は最後の向きを保持）
        Vector2 dpad = input.Dpad;
        if (dpad.sqrMagnitude > 0.01f)
        {
            lastDir = new Vector3(dpad.x, 0f, dpad.y).normalized;
        }

        // クールダウンを減衰
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (input.ButtonA)
        {
            Fire(damage: a * 2, speed: v, range: r);    // 近: 射程r, 威力2a, 速度v
        }
        else if (input.ButtonB)
        {
            Fire(damage: a, speed: v * 1.5f, range: r * 2f);    // 遠: 射程2r, 威力a, 速度1.5v
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
