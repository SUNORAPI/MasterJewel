using UnityEngine;

public class AttackCube : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody Rigidbody;
    private Vector3 direction;
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.linearVelocity = direction * speed;
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

}
