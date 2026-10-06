using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(GameplayInput))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Animator animator;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Collider2D attackTrigger;

    [Header("Movement")]
    [Tooltip("Maximum horizontal speed in world units per second.")]
    [Min(0f)] [SerializeField] private float runSpeed = 6.5f;
    [Min(0f)] [SerializeField] private float jumpSpeed = 12f;
    [Range(0f, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;
    [SerializeField] private bool m_AirControl = true;
    [Range(.01f, .2f)] [SerializeField] private float groundProbeDistance = .08f;
    [Min(0f)] [SerializeField] private float coyoteTime = .1f;
    [Min(0f)] [SerializeField] private float jumpBufferTime = .12f;
    [Range(0f, .9f)] [SerializeField] private float joystickDeadZone = .15f;
    [Min(.01f)] [SerializeField] private float attackDuration = .2f;

    [Header("Events")]
    public UnityEvent OnLandEvent = new UnityEvent();

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
    private readonly HashSet<Component> attackHits = new HashSet<Component>();
    private Rigidbody2D m_Rigidbody2D;
    private Collider2D bodyCollider;
    private GameplayInput input;
    private float smoothingVelocity;
    private float moveH;
    private float jumpBufferedUntil = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float knockbackUntil;
    private float attackTimer;
    private bool attacking;
    private bool clicked;
    private bool m_Grounded;
    private int jumpsUsed;
    private bool m_FacingRight = true;
    private ContactFilter2D groundFilter;
    private Vector2 groundVelocity;
    private float ignoreGroundUntil;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        input = GetComponent<GameplayInput>();
        groundFilter = new ContactFilter2D { useTriggers = false };
        foreach (var collider in GetComponents<Collider2D>())
            if (!collider.isTrigger)
            {
                bodyCollider = collider;
                break;
            }
        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();
        if (attackTrigger != null)
            attackTrigger.enabled = false;
    }

    public void Attack()
    {
        if (Time.timeScale > 0f)
            clicked = true;
    }

    public void Jump()
    {
        if (Time.timeScale > 0f)
            jumpBufferedUntil = Time.time + jumpBufferTime;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            moveH = 0f;
            clicked = false;
            jumpBufferedUntil = float.NegativeInfinity;
            return;
        }
        var touchAxis = joystick != null ? joystick.Horizontal : 0f;
        moveH = Mathf.Abs(touchAxis) >= joystickDeadZone ? touchAxis : input.Horizontal;
        moveH = Mathf.Clamp(moveH, -1f, 1f);
        if (input.JumpPressed)
            Jump();
        if (input.AttackPressed)
            Attack();

        if (clicked && !attacking)
        {
            attacking = true;
            attackTimer = attackDuration;
            attackHits.Clear();
            if (attackTrigger != null)
                attackTrigger.enabled = true;
        }
        clicked = false;
        if (attacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attacking = false;
                if (attackTrigger != null)
                    attackTrigger.enabled = false;
            }
        }
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(m_Rigidbody2D.linearVelocity.x - groundVelocity.x));
            animator.SetBool("IsAttack", attacking);
            animator.SetBool("IsJumping", !m_Grounded);
        }
    }

    private void FixedUpdate()
    {
        var wasGrounded = m_Grounded;
        m_Grounded = false;
        groundVelocity = Vector2.zero;
        if (bodyCollider != null && Time.time >= ignoreGroundUntil)
        {
            // Probe the physical feet, not legacy markers whose offsets differ between scenes.
            var count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundProbeDistance);
            for (var i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.rigidbody == m_Rigidbody2D || hit.normal.y < .6f)
                    continue;
                var platform = hit.collider.GetComponentInParent<MovingPlatform>();
                var onGroundLayer = (m_WhatIsGround.value & (1 << hit.collider.gameObject.layer)) != 0;
                if (!onGroundLayer && platform == null && !hit.collider.CompareTag("Platform"))
                    continue;
                var surfaceVelocity = platform != null ? platform.Velocity
                    : (hit.rigidbody != null ? hit.rigidbody.GetPointVelocity(hit.point) : Vector2.zero);
                if (m_Rigidbody2D.linearVelocity.y - surfaceVelocity.y > .5f)
                    continue;
                m_Grounded = true;
                groundVelocity = surfaceVelocity;
                break;
            }
        }
        if (m_Grounded)
        {
            lastGroundedTime = Time.time;
            jumpsUsed = 0;
            if (!wasGrounded)
                OnLandEvent.Invoke();
        }
        Move(moveH, Time.time <= jumpBufferedUntil);
    }

    // Public for existing scene callbacks and the archived movement component.
    public void Move(float move, bool jump)
    {
        if (Time.timeScale == 0f)
            return;
        if (Time.time >= knockbackUntil && (m_Grounded || m_AirControl))
        {
            var velocity = m_Rigidbody2D.linearVelocity;
            velocity.x = Mathf.SmoothDamp(velocity.x - groundVelocity.x, Mathf.Clamp(move, -1f, 1f) * runSpeed,
                ref smoothingVelocity, m_MovementSmoothing, Mathf.Infinity, Time.fixedDeltaTime) + groundVelocity.x;
            if (m_Grounded)
                velocity.y = groundVelocity.y;
            m_Rigidbody2D.linearVelocity = velocity;
        }
        if ((move > 0f && !m_FacingRight) || (move < 0f && m_FacingRight))
            Flip();
        if (!jump)
            return;

        var canGroundJump = m_Grounded || Time.time - lastGroundedTime <= coyoteTime;
        if (!canGroundJump && jumpsUsed >= 2)
            return;
        if (!canGroundJump && jumpsUsed == 0)
            jumpsUsed = 1;
        m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, jumpSpeed + Mathf.Max(groundVelocity.y, 0f));
        jumpsUsed++;
        lastGroundedTime = float.NegativeInfinity;
        jumpBufferedUntil = float.NegativeInfinity;
        ignoreGroundUntil = Time.time + .08f;
        m_Grounded = false;
    }

    public IEnumerator Knockback(float knockDur, float knockPwr, Vector3 knockDir)
    {
        var duration = Mathf.Max(.12f, knockDur);
        knockbackUntil = Time.time + duration;
        smoothingVelocity = 0f;
        var strength = Mathf.Clamp(knockPwr * .02f, 4f, 10f);
        m_Rigidbody2D.linearVelocity = new Vector2(m_FacingRight ? -strength : strength, strength);
        yield return new WaitForSeconds(duration);
    }

    private void OnDisable()
    {
        moveH = 0f;
        clicked = false;
        attacking = false;
        jumpBufferedUntil = float.NegativeInfinity;
        if (attackTrigger != null)
            attackTrigger.enabled = false;
    }

    private void Flip()
    {
        m_FacingRight = !m_FacingRight;
        var scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D collision) => Hit(collision);
    private void OnTriggerStay2D(Collider2D collision) => Hit(collision);

    private void Hit(Collider2D collision)
    {
        if (!attacking || attackTrigger == null || !attackTrigger.IsTouching(collision))
            return;
        var enemy = collision.GetComponentInParent<Enemy>();
        var boss = collision.GetComponentInParent<Boss>();
        var target = enemy != null ? (Component)enemy : boss;
        if (target == null || !attackHits.Add(target))
            return;
        if (enemy != null)
            enemy.Damage(1);
        else
            boss.Damage(1);
    }
}
