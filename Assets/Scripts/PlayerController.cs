using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 3f;
    [SerializeField] private GameObject attackPrefab;
    private Rigidbody Rigidbody;
    private ControllerInput ControllerInput;
    private Vector3 lookDirection = Vector3.forward;
    private bool pressButtonA = false;

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        ControllerInput = GetComponent<ControllerInput>();
    }

    void Update()
    {        
        Vector3 move = 
        new Vector3(ControllerInput.Dpad.x, 0, ControllerInput.Dpad.y);

        if(move != Vector3.zero)
        {
            lookDirection = move.normalized;
        }
        Rigidbody.linearVelocity = move * MoveSpeed;

        if (ControllerInput.ButtonA && !pressButtonA)
        {
            GameObject cube = Instantiate(attackPrefab, transform.position + lookDirection, Quaternion.identity);
            cube.GetComponent<AttackCube>().SetDirection(lookDirection);
        }
        pressButtonA = ControllerInput.ButtonA;
    }
}