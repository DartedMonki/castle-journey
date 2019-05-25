using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class Boss : MonoBehaviour
{
    [SerializeField] private Transform groundedDetection;
    [SerializeField] private float speed = 2;
    [SerializeField] private float distanceFromPlayer = 20;
    [SerializeField] private bool movingRight = true;
    [SerializeField] private bool facingRight = true;
    [SerializeField] private int health = 10;
    [SerializeField] private int index = 4;
    [SerializeField] private int kPower = 1000;

    private float timeDamage = 1.5f;

    [SerializeField] private Slider healthBar;
    private PlayerController p_control;
    private PlayerHealth p_health;
    private Transform target;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        p_health = FindObjectOfType<PlayerHealth>();
        p_control = FindObjectOfType<PlayerController>();

    }
    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(target.position, transform.position) < distanceFromPlayer)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (target.position.x > transform.position.x && !facingRight) //if the target is to the right of enemy and the enemy is not facing right
                Flip();
            if (target.position.x < transform.position.x && facingRight)
                Flip();

            if (health <= 0)
            {
                Die();
            }

        }

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
            StartCoroutine(p_control.Knockback(0.02f, kPower, p_control.transform.position));
            Debug.Log("Attacking");
        }
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        facingRight = !facingRight;
        movingRight = !movingRight;
    }

    public void Damage(int dmg)
    {
        health -= dmg;
        //gameObject.GetComponent<Animation>().Play("ThiefAttackedRed");

    }

    public void Die()
    {
        gameObject.SetActive(false);
        SceneManager.LoadScene(index);
        //p_score.AddScore(1000);
    }
}
