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
        // 回転凍結 と Y位置固定 を両方指定(|で結合。代入だと後者で上書きされてしまう)
        Rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    //FixedUpdateでフレームレート依存を回避
    void FixedUpdate()
    {
        Rigidbody.linearVelocity =
        new Vector3(ControllerInput.Dpad.x, 0, ControllerInput.Dpad.y) * MoveSpeed;
    }
}