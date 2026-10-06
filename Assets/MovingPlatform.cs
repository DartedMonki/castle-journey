using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public class MovingPlatform : MonoBehaviour
{
    public GameObject player;
    public Transform pos1, pos2;
    [Min(0f)] public float speed = 2f;
    public Transform startPos;
    public Vector2 Velocity { get; private set; }

    private Rigidbody2D body;
    private Vector2 nextPos;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (pos1 == null || pos2 == null)
        {
            Debug.LogError("Assign both moving-platform waypoints.", this);
            enabled = false;
            return;
        }
        nextPos = startPos != null ? (Vector2)startPos.position : (Vector2)pos1.position;
    }

    private void FixedUpdate()
    {
        if ((body.position - (Vector2)pos1.position).sqrMagnitude < .0001f)
            nextPos = pos2.position;
        else if ((body.position - (Vector2)pos2.position).sqrMagnitude < .0001f)
            nextPos = pos1.position;
        var position = Vector2.MoveTowards(body.position, nextPos, speed * Time.fixedDeltaTime);
        Velocity = (position - body.position) / Time.fixedDeltaTime;
        body.MovePosition(position);
    }

    private void OnDisable()
    {
        Velocity = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        if (pos1 != null && pos2 != null)
            Gizmos.DrawLine(pos1.position, pos2.position);
    }
}
