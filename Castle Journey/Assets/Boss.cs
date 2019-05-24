using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    [SerializeField] private int health = 50;
    [SerializeField] private int damage = 5;
    private float timeDamage = 1.5f;

    [SerializeField] private Slider healthBar;
    private PlayerController p_control;
    private PlayerHealth p_health;

    void Start()
    {
        p_health = FindObjectOfType<PlayerHealth>();
        p_control = FindObjectOfType<PlayerController>();

    }
    // Update is called once per frame
    void Update()
    {
        if (timeDamage > 0)
        {
            timeDamage -= Time.deltaTime;
        }

        healthBar.value = health;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag.Equals("Player"))
        {
            p_health.Damage(1);
            StartCoroutine(p_control.Knockback(0.02f, 350, p_control.transform.position));
            Debug.Log("Attacking");
        }
    }
}
