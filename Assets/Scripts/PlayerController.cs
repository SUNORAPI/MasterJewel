using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 3f;
    private Rigidbody Rigidbody;
    private ControllerInput ControllerInput;
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {        
        Rigidbody.linearVelocity = 
        new Vector3(ControllerInput.Dpad.x, 0, ControllerInput.Dpad.y) * MoveSpeed;
    }
}