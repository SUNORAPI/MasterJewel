using UnityEngine;

public class PlayerMoveControll : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 2f; // GridSysがないシーンで使う予備速度
    [SerializeField, Min(0f)] private float gameplaySpeedMultiplier = 2f;
    private Rigidbody Rigidbody;
    private ControllerInput ControllerInput;
    private PlayerRegistrar Registrar;
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        ControllerInput = GetComponent<ControllerInput>();
        Registrar = GetComponent<PlayerRegistrar>();
        Rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    //FixedUpdateでフレームレート依存を回避
    void FixedUpdate()
    {
        Vector2 dpad = ControllerInput.Dpad;
        // HIDジョイスティックのDpadを正規化
        Vector3 dir = dpad.sqrMagnitude > 0.01f
            ? new Vector3(dpad.x, 0, dpad.y).normalized
            : Vector3.zero;

        if (dir.sqrMagnitude > 0f && Registrar != null)
            Registrar.SetFacingDirection(dir);

        // 基準速度の2倍 = 2マス/秒
        float moveSpeed = GridSys.Instance != null ? GridSys.Instance.CellSize : MoveSpeed;
        Rigidbody.linearVelocity = dir * moveSpeed * gameplaySpeedMultiplier;
    }
}
