using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Joystick joystick;
    [SerializeField] private float runSpeed = 20f;

    
    private PlayerAttack p_Attack;
    private float moveH = 0f;
    private bool jump = false;

    void Awake()
    {
        p_Attack = GetComponent<PlayerAttack>();
    }
    // Update is called once per frame
    void Update()
    {
        // moveH = Input.GetAxisRaw("Horizontal") * runSpeed ;
        if (joystick.Horizontal >= .2f)
        {
            moveH = runSpeed;
        }
        else if (joystick.Horizontal <= -.2f)
        {
            moveH = -runSpeed;
        }
        else moveH = 0f;

        animator.SetFloat("Speed", Mathf.Abs(moveH));
        
        if (Input.GetButtonDown("Jump"))
        {
            jump = true;
        }
    }
    
    public void Jump()
    {
        jump = true;
    }


    void FixedUpdate()
    {
        controller.Move(moveH * Time.fixedDeltaTime, jump);
        jump = false;
    }
}

