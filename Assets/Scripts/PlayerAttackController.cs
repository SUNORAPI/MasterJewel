using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerHPManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    int Damage;
    void Start()
    {
        
    } 
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            Destroy(other.gameObject);
            int myHealth = GetComponent<PlayerStatus.health>();
            myHealth = myHealth - Damage;

        }
    }

}
