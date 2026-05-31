using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;



public class EnemyAI : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player;
    public float sightRange = 8f;
    public float sightAngle = 90f;   // degrees, total cone width
    public float searchTimeout = 4f;    // seconds before giving up and going Idle
    PlayerMovement playerMovement;
    List<Collider2D> lastSeenPlayerPlatforms;

    [Header("Platform Detection")]
    public string platformLayerName = "groundLayer";
    public LayerMask platformLayer;
    public float detectionRadius = 5f;
    public int rayCount = 25; // Number of rays in the half-circle
    private Collider2D currentPlatform;
    private bool isJumping = false;


    int directionX = 1;
    public float speed = 3f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private bool isSpriteFlipped = false;
    [SerializeField] private List<Sprite> enemySprites; // Assign in inspector: 0 = Idle, 1 = Search, 2 = Chase


    public enum EnemyStateType { Idle, Search, Chase }


    // Shared data states can read/write
    public float timeWhenPlayerSeenLast { get; set; }
    public float StateEnterTime { get; private set; }

    EnemyState _current;
    public EnemyStateType CurrentStateType { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        playerMovement = player.gameObject.GetComponent<PlayerMovement>();
        SetState(EnemyStateType.Idle);
    }

    void Update()
    {
        _current?.UpdateState();

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


    public bool CanSeePlayer()
    {
        if (player == null) return false;

        // If player is outside of sight range, no need to check angle or line of sight
        Vector2 toPlayer = player.position - transform.position;
        //Debug.Log($"[EnemyAI] Checking sight: Player at {player.position}, Enemy at {transform.position}, ToPlayer vector: {toPlayer}, Magnitude: {toPlayer.magnitude}");
        if (toPlayer.magnitude > sightRange) return false;

        // Facing direction based on velocity
        Vector2 facing = GetComponent<Rigidbody2D>().linearVelocity.normalized;
        //Debug.Log($"[EnemyAI] Facing direction: {facing}");
        if (facing == Vector2.zero) facing = Vector2.right;

        // Angle check to see if player is within sight cone
        float angle = Vector2.Angle(facing, toPlayer);
        //Debug.Log($"[EnemyAI] Angle to player: {angle} degrees (Sight cone: {sightAngle} degrees)");
        if (angle > sightAngle * 0.5f) return false;

        // Line-of-sight check
        var hit = Physics2D.Raycast(transform.position, toPlayer.normalized, sightRange, LayerMask.GetMask("Default", "groundLayer"));
        //Debug.Log($"[EnemyAI] Line-.Raycast hit: {(hit.collider != null ? hit.collider.name : "None")} (Layer: {(hit.collider != null ? LayerMask.LayerToName(hit.collider.gameObject.layer) : "N/A")})");
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    public void SetLastSeenPlayerTime() => timeWhenPlayerSeenLast = Time.time;

    public void SetLastSeenPlayerPlatforms() => lastSeenPlayerPlatforms = playerMovement.traversedPlatforms;

    public void MoveAlongPlatform()
    {
        directionX = (directionX == -1) ? 1 : -1;
        rb.linearVelocityX = directionX * speed;
    }

    public void CheckForPlatformEdge()
    {
        if (isJumping) return;

        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.5f; // Adjust as needed
        Vector2 direction = Vector2.down;
        Vector2 movementDirection;

        if (CurrentStateType == EnemyStateType.Chase && player != null && currentPlatform == lastSeenPlayerPlatforms[lastSeenPlayerPlatforms.Count - 1])
        {
            Vector2 toPlayer = player.position - transform.position;

            if (Mathf.Abs(toPlayer.x) > 0.8f)
            {
                movementDirection = new Vector2(toPlayer.x, 0).normalized;
                directionX = (movementDirection.x > 0) ? 1 : -1;
            }
            else
            {
                movementDirection = Vector2.zero; // Player is directly above/below, so no horizontal movement
                directionX = 0;
            }

        }
        else
        { 
            movementDirection = new Vector2(directionX, 0).normalized;
        } 

        
        
        
        float distance = 1f; // Adjust as needed
        var bounds = spriteRenderer.bounds;

        Vector2 locationForRaycast = origin + (movementDirection * bounds.extents.x);

        var hit = Physics2D.Raycast(locationForRaycast, direction, distance, platformLayer);
        //Debug.Log($"[EnemyAI] Raycast hit: {(hit.collider != null ? hit.collider.name : "None")} (Layer: {(hit.collider != null ? LayerMask.LayerToName(hit.collider.gameObject.layer) : "N/A")})");
        //Debug.Log($"Current platform: {(currentPlatform != null ? currentPlatform.name : "None")}");
        //Debug.Log("hit collider layer " + hit.collider.gameObject.layer);
        //Debug.Log("Platform layer " + LayerMask.NameToLayer(platformLayerName));

        if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer(platformLayerName))
        {
            rb.linearVelocityX = directionX * speed;
            //Debug.Log("Movement along platform started");
            currentPlatform = hit.collider;
        } else if (hit.collider == null)
        {
            // No ground ahead, reverse direction
            isSpriteFlipped = !isSpriteFlipped;
            spriteRenderer.flipX = isSpriteFlipped;

            List<RaycastHit2D> jumpHits = CheckForJump(movementDirection);
            if (jumpHits.Count > 0)
            {
                
                InitiateJump(jumpHits);
            }
            else
            {
                currentPlatform = hit.collider;
                MoveAlongPlatform();
            }
        }
    }
    
    public void SwitchEnemySprite(EnemyStateType enemyState)
    {
        switch (enemyState)
        {
            case EnemyStateType.Idle:
                spriteRenderer.sprite = enemySprites[0]; 
                break;
            case EnemyStateType.Search:
                spriteRenderer.sprite = enemySprites[1];
                break;
            case EnemyStateType.Chase:
                spriteRenderer.sprite = enemySprites[2];
                break;
        }
    }


    public List<RaycastHit2D> CheckForJump(Vector2 movementDirection)
    {
        float baseAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
        var hits = new List<RaycastHit2D>();
        var seen = new HashSet<Collider2D>();

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = baseAngle - 80f + (160f / rayCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D[] allHits = Physics2D.RaycastAll(transform.position, rayDir, detectionRadius, platformLayer);

            foreach (RaycastHit2D hit in allHits)
            {
                if (hit.collider != currentPlatform && !seen.Contains(hit.collider))
                {
                    hits.Add(hit);
                    seen.Add(hit.collider);
                }
            }
        }

        return hits;
    }

    public void InitiateJump(List<RaycastHit2D> jumpHits)
    {
        RaycastHit2D target = FindTargetPlatform(jumpHits, CurrentStateType);

        // Find the top surface + closest horizontal edge of that platform
        Bounds b = target.collider.bounds;
        float landingSpotOffset = 0.15f; // Adjust as needed to avoid landing right on the edge
        float closestEdgeX = (Mathf.Abs(b.min.x - transform.position.x) < Mathf.Abs(b.max.x - transform.position.x)) ? b.min.x + b.extents.x * landingSpotOffset : b.max.x - b.extents.x * landingSpotOffset;

        Vector2 landingTarget = new Vector2(closestEdgeX, b.max.y + spriteRenderer.bounds.extents.y);

        StartCoroutine(JumpArc(landingTarget));
    }

    private RaycastHit2D FindTargetPlatform(List<RaycastHit2D> jumpHits, EnemyStateType currentState)
    {
        RaycastHit2D bestHit = jumpHits[0];
        //Debug.Log($"[EnemyAI] Evaluating {jumpHits.Count} jump targets in state {CurrentStateType}");
        //Debug.Log($"[EnemyAI] hit platforms: {string.Join(", ", jumpHits.ConvertAll(h => h.collider.name))}");


        switch (currentState)
        {
            case EnemyStateType.Idle:
                // In Idle, choose random platform to jump to
                bestHit = jumpHits[UnityEngine.Random.Range(0, jumpHits.Count - 1)];
                break;
            default:
                // In Search or Chase, prefer platforms that the player has recently been on

                Dictionary<Collider2D, int> filteredHits = new Dictionary<Collider2D, int>();

                for (int i = 0; i < jumpHits.Count; i++)
                {
                    int index = lastSeenPlayerPlatforms.IndexOf(jumpHits[i].collider);
                    if (index != -1)
                    {
                        filteredHits.Add(jumpHits[i].collider, index);
                    }
                }

                if (filteredHits.Count > 0)
                {
                    // Sort by how recently the player was on that platform
                    var sortedHits = new List<KeyValuePair<Collider2D, int>>(filteredHits);
                    sortedHits.Sort((a, b) => b.Value.CompareTo(a.Value));
                    // Take the most recent one
                    Collider2D targetCollider = sortedHits[0].Key;
                    bestHit = jumpHits.Find(h => h.collider == targetCollider);
                }
                else
                {
                    bestHit = jumpHits[UnityEngine.Random.Range(0, jumpHits.Count - 1)];
                }

                break;
        }
        return bestHit;
    }


    private IEnumerator JumpArc(Vector2 target)
    {
        isJumping = true;
        Vector2 start = transform.position;
        float duration = CalculateJumpDuration(start, target);
        float elapsed = 0f;
        enemyCollider.enabled = false;

        // Disable normal movement while jumping
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lerp X linearly, arc Y with a parabola
            float x = Mathf.Lerp(start.x, target.x, t);
            float y = Mathf.Lerp(start.y, target.y, t) + ArcHeight(start.y, target.y) * 4f * t * (1f - t);

            rb.MovePosition(new Vector2(x, y));
            yield return null;
        }

        // Snap to target and restore physics
        rb.MovePosition(target);
        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.zero;
        enemyCollider.enabled = true;
        isJumping = false;
    }

    private float CalculateJumpDuration(Vector2 start, Vector2 target)
    {
        float dist = Vector2.Distance(start, target);
        float heightDiff = target.y - start.y;

        // Base time on distance, but add extra time for big upward jumps
        float baseDuration = dist / speed;
        float heightPenalty = Mathf.Max(0f, heightDiff * 0.15f);
        return Mathf.Clamp(baseDuration + heightPenalty, 0.4f, 1.5f);
    }

    private float ArcHeight(float startY, float targetY)
    {
        // Always arc at least 1.5 units above the higher of the two points
        return Mathf.Max(1.5f, Mathf.Abs(targetY - startY) * 0.5f + 1.5f);
    }

}
