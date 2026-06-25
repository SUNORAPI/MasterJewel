using UnityEngine;

public class PlayerMoveControll : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 3f;
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
        Rigidbody.linearVelocity =
        new Vector3(ControllerInput.Dpad.x, 0, ControllerInput.Dpad.y) * MoveSpeed;
    }
}