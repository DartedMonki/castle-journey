using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Collider2D attackTrigger;
    public Animator animator;
    public bool attacking = false;
    
    private float attackTimer = 0.2f;
    private bool clicked = false;
    
    public void ButtonClick()
    {
        clicked = true;
    }

    void Awake()
    {
        attackTrigger.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if ( (Input.GetButtonDown("Fire1") || clicked) && !attacking)
        {
            attacking = true;
            attackTimer = 0.2f;
            attackTrigger.enabled = true;
            clicked = false;
            
        }
        

        if (attacking)
        {
            if(attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }
            else
            {
                attacking = false;
                attackTrigger.enabled = false;
                
            }
        }
        
        animator.SetBool("IsAttack", attacking);

    }

}
