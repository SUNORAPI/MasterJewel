using UnityEngine;

public class PlayerMoveControll : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 2f;
    private Rigidbody Rigidbody;
    private ControllerInput ControllerInput;
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        ControllerInput = GetComponent<ControllerInput>();
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

        Rigidbody.linearVelocity = dir * MoveSpeed;
    }
}