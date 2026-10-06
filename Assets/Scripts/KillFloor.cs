using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFloor : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            collision.GetComponentInParent<PlayerHealth>()?.Damage(3);
            
        }
        if(collision.tag == "Enemy")
        {
            collision.GetComponentInParent<Enemy>()?.Damage(3);
            
        }
    }
}
