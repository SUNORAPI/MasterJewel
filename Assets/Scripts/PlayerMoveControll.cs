using UnityEngine;

public class PlayerMoveControll : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 2f; // GridSysがないシーンで使う予備速度
    [SerializeField, Min(0f)] private float gameplaySpeedMultiplier = 2f;
    private Rigidbody Rigidbody;
    private Collider PlayerCollider;
    private ControllerInput ControllerInput;
    private PlayerRegistrar Registrar;
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        PlayerCollider = GetComponent<Collider>();
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
        Vector3 desiredVelocity = dir * moveSpeed * gameplaySpeedMultiplier;

        var grid = GridSys.Instance;
        if (grid != null)
        {
            Vector2 halfExtents = GetHorizontalHalfExtents();
            Vector3 currentPosition = grid.ClampToField(Rigidbody.position, halfExtents);
            if ((currentPosition - Rigidbody.position).sqrMagnitude > 0.000001f)
                Rigidbody.position = currentPosition;

            // 次の物理更新位置を先に制限し、高速移動でも盤面を飛び越えないようにする。
            float step = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            Vector3 nextPosition = currentPosition + desiredVelocity * step;
            Vector3 clampedNextPosition = grid.ClampToField(nextPosition, halfExtents);
            desiredVelocity = (clampedNextPosition - currentPosition) / step;
        }

        Rigidbody.linearVelocity = desiredVelocity;
    }

    Vector2 GetHorizontalHalfExtents()
    {
        if (PlayerCollider == null) return Vector2.zero;
        Vector3 extents = PlayerCollider.bounds.extents;
        return new Vector2(extents.x, extents.z);
    }
}
