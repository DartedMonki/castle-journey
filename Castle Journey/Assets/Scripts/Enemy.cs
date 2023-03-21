using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Transform groundedDetection;
    [SerializeField] private float speed = 2;
    [SerializeField] private float distanceFromPlayer = 10;
    [SerializeField] private bool movingRight = true;
    [SerializeField] private bool facingRight = true;
    [SerializeField] private int enemyLife = 3;

    private PlayerHealth p_health;
    private Score p_score;
    private PlayerController p_control;
    private Animator anim;
    private Transform target;

    void Start()
    {
        anim = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        p_health = FindObjectOfType<PlayerHealth>();
        p_control = FindObjectOfType<PlayerController>();
        p_score = FindObjectOfType<Score>();
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

        if (enemyLife <= 0)
        {
            Die();
        }
        }
        anim.SetFloat("Speed", Mathf.Abs(speed));
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        
        if (col.gameObject.tag.Equals("Player"))
        {
            anim.SetBool("IsAttack", true);
            if (!p_health.isInvincible)
            {
                p_health.Damage(1);
                StartCoroutine(p_control.Knockback(0.02f, 350, p_control.transform.position));
            }
            Debug.Log("Attacking");
        }
        else anim.SetBool("IsAttack", false);
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
        enemyLife -= dmg;
        gameObject.GetComponent<Animation>().Play("ThiefAttackedRed");
        
    }

    public void Die()
    {
        gameObject.SetActive(false);
        p_score.AddScore(1000);
    }
}