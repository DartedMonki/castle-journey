using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFloor : MonoBehaviour
{
    private PlayerHealth health;

	void Start()
	{
        health = FindObjectOfType<PlayerHealth>();
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            health.Health = 0;
        }
    }
}
