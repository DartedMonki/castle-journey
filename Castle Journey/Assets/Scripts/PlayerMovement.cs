using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public PlayerController controller;
    public Animator animator;
    public Joystick joystick;
    public Joybutton joybutton;

    public float runSpeed = 20f;

    float moveH = 0f;
    bool jump = false;

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
        
        // if (Input.GetButtonDown("Jump"))
        if (joybutton.Pressed || Input.GetButtonDown("Jump"))
        {
            jump = true;
            animator.SetBool("IsJumping", true);
        }
    }

    public void Landing()
    {
        animator.SetBool("IsJumping", false);
    }

    void FixedUpdate()
    {
        controller.Move(moveH * Time.fixedDeltaTime, jump);
        jump = false;
    }
}

