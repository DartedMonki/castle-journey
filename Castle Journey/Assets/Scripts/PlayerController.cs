using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerController : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;							
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;
	[SerializeField] private bool m_AirControl = false;	
	[SerializeField] private LayerMask m_WhatIsGround;						
	[SerializeField] private Transform m_GroundCheck;
	[SerializeField] private Animator animator;

    const float k_GroundedRadius = 1f; 
	private bool m_Grounded;         
	private int m_Doublejump;           
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true; 
	private Vector3 m_Velocity = Vector3.zero;

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;
	
	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }


	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			{
				OnLandEvent = new UnityEvent();
			}

	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				

				if ((!wasGrounded && m_Rigidbody2D.velocity.y < 0) )
					OnLandEvent.Invoke();
					animator.SetBool("IsJumping", !m_Grounded);
					
			}
		}
	}


	public void Move(float move, bool jump)
	{
		if (m_Grounded == false)
		animator.SetBool("IsJumping", true);
		
		if (m_Grounded || m_AirControl)
		{

			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);

			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			if (move > 0 && !m_FacingRight)
			{
				Flip();
			}
	
			else if (move < 0 && m_FacingRight)
			{
				Flip();
			}
		}

		if (jump)
		{
			
			if(m_Grounded)
			{
				m_Doublejump = 0;
			}
			if(m_Grounded || m_Doublejump < 2)
			{
				
				m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x,0);
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
				m_Doublejump += 1;
				m_Grounded = false;
			}
			
		}
	}

    public IEnumerator Knockback(float knockDur, float knockPwr, Vector3 knockDir)
    {
        float timer = 0;
        m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0);
        while (knockDur > timer)
        {
            timer += Time.deltaTime;
            if (m_FacingRight)
            {
                m_Rigidbody2D.AddForce(new Vector3(knockDir.x * -50, knockDir.y + knockPwr, transform.position.z));
            }
            else
            {
                m_Rigidbody2D.AddForce(new Vector3(knockDir.x * 50, knockDir.y + knockPwr, transform.position.z));
            }
        }
        yield return 0;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag.Equals("Platform"))
        {
            Debug.Log("OnPlatform");
            this.transform.parent = col.transform;
        }
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.tag.Equals("Platform"))
        {
            Debug.Log("LeavingPlatform");
            this.transform.parent = null;
        }
    }

    private void Flip()
	{
		m_FacingRight = !m_FacingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
