using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFloor : MonoBehaviour
{
    private PlayerHealth health;
    private Enemy enemy;

	void Start()
	{
        health = FindObjectOfType<PlayerHealth>();
        enemy = FindObjectOfType<Enemy>();
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            health.Damage(3);
            
        }
        if(collision.tag == "Enemy")
        {
            enemy.Damage(3);
            
        }
    }
}
