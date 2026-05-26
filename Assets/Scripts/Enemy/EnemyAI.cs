using UnityEditor.Experimental.GraphView;
using UnityEngine;



public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public Transform player;
    public float sightRange = 8f;
    public float sightAngle = 90f;   // degrees, total cone width
    public float searchTimeout = 4f;    // seconds before giving up and going Idle
    int directionX = 1;
    private Rigidbody2D rb;


    public enum EnemyStateType { Idle, Search, Chase }


    // Shared data states can read/write
    public Vector2 LastKnownPlayerPos { get; set; }
    public float StateEnterTime { get; private set; }

    EnemyState _current;
    public EnemyStateType CurrentStateType { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SetState(EnemyStateType.Idle);
    }

    void Update()
    {
        _current?.UpdateState();
        CheckForPlatformEdge();
    }

    public void SetState(EnemyStateType type)
    {
        _current?.ExitState();
        CurrentStateType = type;
        StateEnterTime = Time.time;

        _current = type switch
        {
            EnemyStateType.Idle => new IdleState(this),
            EnemyStateType.Search => new SearchState(this),
            EnemyStateType.Chase => new ChaseState(this),
            _ => new IdleState(this)
        };

        _current.EnterState();
        Debug.Log($"[EnemyAI] → {type}");
    }

    // How long we've been in the current state
    public float TimeInState => Time.time - StateEnterTime;

    // Shared perception — states call this rather than duplicating logic
    public bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > sightRange) return false;

        // Facing direction based on velocity (swap for a facing field if you prefer)
        Vector2 facing = GetComponent<Rigidbody2D>().linearVelocity.normalized;
        if (facing == Vector2.zero) facing = Vector2.right;

        float angle = Vector2.Angle(facing, toPlayer);
        if (angle > sightAngle * 0.5f) return false;

        // Line-of-sight check
        var hit = Physics2D.Raycast(transform.position, toPlayer.normalized,
                                    sightRange, LayerMask.GetMask("Default", "Platform"));
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    public void SetTargetPosition(Vector2 pos) => LastKnownPlayerPos = pos;

    public void MoveAlongPlatform()
    {
        directionX = (directionX == -1) ? 1 : -1;

        //directionX = (targetPos.x - transform.position.x > 0.01) ? 1 : -1;
        rb.linearVelocityX = directionX * 3f; // Adjust speed as needed
    }

    public void CheckForPlatformEdge()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.5f; // Adjust as needed
        Vector2 direction = Vector2.down;
        float distance = 1f; // Adjust as needed
        var hit = Physics2D.Raycast(origin, direction, distance, LayerMask.GetMask("groundLayer"));
        Debug.Log($"[EnemyAI] Raycast hit: {(hit.collider != null ? hit.collider.name : "None")} (Layer: {(hit.collider != null ? LayerMask.LayerToName(hit.collider.gameObject.layer) : "N/A")})");
        if (hit.collider == null)
        {
            // No ground ahead, reverse direction
            MoveAlongPlatform();
        }
        else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("groundLayer"))
        {
            // Hit object is on the "groundLayer" — treat as platform/player present, switch to chase state
            rb.linearVelocityX = directionX * 3f;
        }
    }
    
    
    /*public void MoveAlongPlatform()
    {
        Vector2 targetPos = new Vector2(1, 1);

        // Simple horizontal movement towards targetPos, respecting platforms
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        float speed = 300f; // Adjust as needed
        // Check for platform edges before moving
        Vector2 nextPos = (Vector2)transform.position + direction * speed * Time.deltaTime;
        var hit = Physics2D.Raycast(transform.position, direction, speed * Time.deltaTime, LayerMask.GetMask("Default", "Platform"));
        if (hit.collider == null || hit.collider.CompareTag("Player"))
        {
            transform.position = nextPos;
        }
    }*/
}
