using UnityEngine;

public class PlayerMoveControll : MonoBehaviour
{
    // 2秒で1マス(cellSize=4)移動する速度: 4 units / 2s = 2 unit/sec
    [SerializeField] private float MoveSpeed = 2f;
    private Rigidbody Rigidbody;
    private ControllerInput ControllerInput;
    private Vector3 lastLoggedPos; // TODO(temp debug)
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        ControllerInput = GetComponent<ControllerInput>();
        Rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        lastLoggedPos = transform.position; // TODO(temp debug)
    }

    //FixedUpdateでフレームレート依存を回避
    void FixedUpdate()
    {
        Vector2 dpad = ControllerInput.Dpad;
        // HIDジョイスティックのDpadは生値の大きさが小さいため、方向のみを取り出して正規化する
        Vector3 dir = dpad.sqrMagnitude > 0.01f
            ? new Vector3(dpad.x, 0, dpad.y).normalized
            : Vector3.zero;

        Rigidbody.linearVelocity = dir * MoveSpeed;

        // TODO(temp debug): 原因切り分け用。確認後に削除する。
        if (dpad.sqrMagnitude > 0.0001f && Time.frameCount % 10 == 0)
        {
            Vector3 pos = transform.position;
            Vector3 delta10 = pos - lastLoggedPos; // 前回ログ(10フレーム前)からの実移動量
            lastLoggedPos = pos;
            Debug.Log(
                $"[MoveDebug] rawDpad={dpad:F4} dir={dir:F4} velocity={Rigidbody.linearVelocity:F4} " +
                $"pos={pos:F6} delta(10frame)={delta10:F6} isKinematic={Rigidbody.isKinematic} " +
                $"constraints={Rigidbody.constraints} timeScale={Time.timeScale} fixedDeltaTime={Time.fixedDeltaTime}");
        }
    }
}