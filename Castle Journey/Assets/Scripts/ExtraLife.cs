using UnityEngine;
using System.Collections;

public class ExtraLife : MonoBehaviour
{
    [SerializeField] private int livesToGive = 1;
    
    private PlayerHealth health;

    // Use this for initialization
	void Start()
	{
        health = FindObjectOfType<PlayerHealth>();
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            health.AddLife(livesToGive);
            gameObject.SetActive(false);
        }
    }
}