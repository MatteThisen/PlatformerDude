using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    public float speed = 5f;
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check Parameters")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 5f;
    public List<Collider2D> traversedPlatforms;

    [Header("Hook And Spline Parameters")]
    [SerializeField] private float splineStartCheckRadius;
    [SerializeField] private float splineRaycastRadius;
    private Vector2 splineVelocityCalcPoint1;
    private Vector2 splineVelocityCalcPoint2;
    public Vector2 splineExitVelocity;
    private float hookUpStartNormPosOnSpline;
    private float hookDownStartNormPosOnSpline;
    private int currentUpSplineIndex;
    private int currentDownSplineIndex;

    private enum HookState { None, Up, UpAndDown }
    private HookState currentHookState = HookState.None;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private bool isGrounded;
    private PlayerInput playerInput;

    [SerializeField] private GameObject hookUpObject;
    [SerializeField] private GameObject hookDownObject;
    private GameObject hookUpSprite;
    private GameObject hookUpRope;
    private GameObject hookDownSprite;
    private GameObject hookDownRope;
    private TargetJoint2D hookJoint;

    private bool isHookUpActive;
    private bool isHookDownActive;

    private SplineContainer[] allSplines;
    private SplineContainer currentUpSpline;
    private SplineContainer currentDownSpline;

    private bool isDead = false;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        hookJoint = GetComponent<TargetJoint2D>();

        hookUpSprite = hookUpObject.GetComponentInChildren<SpriteRenderer>().gameObject;
        hookUpRope = hookUpObject.GetComponentInChildren<LineRenderer>().gameObject;
        hookDownSprite = hookDownObject.GetComponentInChildren<SpriteRenderer>().gameObject;
        hookDownRope = hookDownObject.GetComponentInChildren<LineRenderer>().gameObject;

        if (hookUpObject == null || hookUpSprite == null || hookUpRope == null || hookDownObject == null || hookDownSprite == null || hookDownRope == null)
        {
            Debug.LogError("Hook objects and/or children not found.");
        }
        else
        {
            hookUpObject.SetActive(false);
            hookUpSprite.SetActive(false);
            hookUpRope.SetActive(false);
            hookDownObject.SetActive(false);
            hookDownSprite.SetActive(false);
            hookDownRope.SetActive(false);
        }


        splineVelocityCalcPoint1 = transform.position;

        allSplines = FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);

    }

    public void OnJump()
    {
        if (isGrounded)
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    void Update()
    {

        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();

        if (playerInput.actions["HookUp"].WasPressedThisFrame())
        {
            switch (currentHookState)
            {
                case HookState.None:
                    ConnectToSpline(hookUpObject);
                    break;
                case HookState.Up:
                    DisconnectFromSpline(hookUpObject);
                    break;
                case HookState.UpAndDown:
                    DisconnectOnlyUpHook();
                    break;
            }
        }

        if (playerInput.actions["HookDown"].WasPressedThisFrame() && currentHookState != HookState.None)
        {
            switch (currentHookState)
            {
                case HookState.Up:
                    ConnectToSpline(hookDownObject);
                    break;
                case HookState.UpAndDown:
                    DisconnectFromSpline(hookDownObject);
                    break;
            }
        }
    }
    void FixedUpdate()
    {
        ApplyMovement();

        splineVelocityCalcPoint1 = splineVelocityCalcPoint2;
        splineVelocityCalcPoint2 = transform.position;

        if (currentHookState != HookState.None)
        {
            Vector2 distanceTravelled = splineVelocityCalcPoint2 - splineVelocityCalcPoint1;
            splineExitVelocity = distanceTravelled / Time.fixedDeltaTime;

            rb.gravityScale = 0;

            Vector2 playerVelocity = rb.linearVelocity;

            HookUpdater();
        }
        else
        {
            CheckGroundStatus();
        }
    }

    void ApplyMovement()
    {
        if (isDead) return;
        
        if (moveInput.x != 0)
        {
            float targetSpeed = moveInput.x * maxSpeed;
            float speedDiff = targetSpeed - rb.linearVelocity.x;
            float movementForce = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float force = speedDiff * movementForce;

            if (!isGrounded)
            {
                force *= 0.5f;
            }


            rb.AddForce(new Vector2(force, 0), ForceMode2D.Force);
        }
        else
        {

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0, deceleration * Time.fixedDeltaTime), rb.linearVelocity.y);
            }
        }
    }

    void DisconnectFromSpline(GameObject hookObject)
    {
        SplineBoxBehaviour splineBoxBehaviour = null;

        if (hookObject == hookUpObject)
        {
            currentHookState = HookState.None;
            isHookUpActive = false;
            hookUpSprite.SetActive(false);
            hookUpRope.SetActive(false);
            splineBoxBehaviour = currentUpSpline.GetComponentInParent<SplineBoxBehaviour>();
            currentUpSpline = null;
            hookUpStartNormPosOnSpline = 0f;
        }
        else
        {
            currentHookState = HookState.Up;
            isHookDownActive = false;
            hookDownSprite.SetActive(false);
            hookDownRope.SetActive(false);
            splineBoxBehaviour = currentDownSpline.GetComponentInParent<SplineBoxBehaviour>();
            currentDownSpline = null;
            hookDownStartNormPosOnSpline = 0f;
        }

        SplineAnimate splineAnimate = hookObject.GetComponent<SplineAnimate>();

        splineAnimate.Pause();
        splineAnimate.enabled = false;
        Debug.Log("Disconnected from SplineBox");

        if (splineBoxBehaviour != null)
        {
            //splineBoxBehaviour.StopNote();
        }
        splineBoxBehaviour = null;

        hookObject.SetActive(false);

        hookJoint.enabled = false;
        rb.gravityScale = 1;
        rb.linearVelocity = splineExitVelocity;
    }

    void ConnectToSpline(GameObject hookObject)
    {

        if (!FindClosestSplinePoint(hookObject))
        {
            return;
        }

        SplineAnimate splineAnimate = hookObject.GetComponent<SplineAnimate>();

        splineAnimate.Container = hookObject == hookUpObject ? allSplines[currentUpSplineIndex] : allSplines[currentDownSplineIndex];
        splineAnimate.StartOffset = hookObject == hookUpObject ? hookUpStartNormPosOnSpline : hookDownStartNormPosOnSpline;
        splineAnimate.enabled = true;
        hookObject.SetActive(true);
        SplineBoxBehaviour splineBoxBehaviour = null;

        splineAnimate.Restart(false);
        splineAnimate.Play();
        Debug.Log("Collided with SplineBox");

        if (hookObject == hookUpObject)
        {
            hookJoint.enabled = true;
            currentHookState = HookState.Up;
            isHookUpActive = true;
            hookUpRope.SetActive(true);
            hookUpSprite.SetActive(true);
            splineBoxBehaviour = currentUpSpline.GetComponentInParent<SplineBoxBehaviour>();
        }
        else
        {
            currentHookState = HookState.UpAndDown;
            isHookDownActive = true;
            hookDownRope.SetActive(true);
            hookDownSprite.SetActive(true);
            splineBoxBehaviour = currentDownSpline.GetComponentInParent<SplineBoxBehaviour>();
        }

        //splineBoxBehaviour.PlayNote();
    }

    void HookUpdater()
    {
        if (hookJoint != null || isHookUpActive)
        {

            SplineAnimate hookUpSplineAnimate = hookUpObject.GetComponent<SplineAnimate>();
            // t finds the normalized time along the spline, as well as the offset from the start of the spline found in FindClosestSplinePoint
            float tUp = hookUpSplineAnimate.NormalizedTime + hookUpSplineAnimate.StartOffset;

            if (tUp > 0.99f)
            {
                if (isHookDownActive)
                {
                    DisconnectOnlyUpHook();
                    return;
                }
                else
                {
                    DisconnectFromSpline(hookUpObject);
                    return;
                }

            }

            // Get the position along the spline
            Vector3 positionOnSpline = currentUpSpline.Spline.EvaluatePosition(tUp);

            Vector3 targetPosition = positionOnSpline + currentUpSpline.GetComponentInParent<Transform>().position;

            hookJoint.target = new Vector2(targetPosition.x, targetPosition.y);

        }

        if (isHookDownActive)
        {
            SplineAnimate hookDownSplineAnimate = hookDownObject.GetComponent<SplineAnimate>();
            // t finds the normalized time along the spline, as well as the offset from the start of the spline found in FindClosestSplinePoint
            float tDown = hookDownSplineAnimate.NormalizedTime + hookDownStartNormPosOnSpline;

            if (tDown > 0.99f)
            {
                DisconnectFromSpline(hookDownObject);
            }
        }
    }

    bool FindClosestSplinePoint(GameObject hookObject)
    {
        bool isSplineWithinRange = false;
        float minDist = float.MaxValue;
        float hookStartNormPosOnSpline = 0f;
        SplineContainer container = null;

        Vector2 raycastDir = (hookObject == hookUpObject) ? Vector2.up : Vector2.down;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, splineRaycastRadius, raycastDir, 200f, LayerMask.GetMask("SplineBox"));

        if (hit.collider != null)
        {
            container = hit.collider.GetComponentInParent<SplineContainer>();
            if (container != null)
            {
                int samples = 50;
                for (int i = 0; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    Vector3 point = container.EvaluatePosition(t);
                    float dist = Vector2.Distance(transform.position, point);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        hookStartNormPosOnSpline = t;
                    }
                }
            }
        }


        Vector3 pointOfEntry = container != null ? container.EvaluatePosition(hookStartNormPosOnSpline) : Vector3.positiveInfinity;

        if (Vector2.Distance(transform.position, pointOfEntry) < splineStartCheckRadius)
        {
            Debug.Log($"Closest spline found at t={hookStartNormPosOnSpline}, distance={minDist}");
            if (hookObject == hookUpObject)
            {
                currentUpSpline = container;
                currentUpSplineIndex = System.Array.IndexOf(allSplines, container);
                hookUpStartNormPosOnSpline = hookStartNormPosOnSpline;
                Debug.Log($"Up spline index: {currentUpSplineIndex}");
            }
            else
            {
                currentDownSpline = container;
                currentDownSplineIndex = System.Array.IndexOf(allSplines, container);
                hookDownStartNormPosOnSpline = hookStartNormPosOnSpline;
                Debug.Log($"Down spline index: {currentDownSplineIndex}");
            }
            isSplineWithinRange = true;
        }
        else
        {
            isSplineWithinRange = false;
            Debug.Log("No spline found within range");
        }

        return isSplineWithinRange; // Return whether a spline was found within range
    }

    void DisconnectOnlyUpHook()
    {
       // currentUpSpline.GetComponentInParent<SplineBoxBehaviour>().StopNote();
        currentHookState = HookState.Up;
        currentUpSplineIndex = currentDownSplineIndex;
        currentUpSpline = currentDownSpline;
        SplineAnimate hookUpSplineAnimate = hookUpObject.GetComponent<SplineAnimate>();
        SplineAnimate hookDownSplineAnimate = hookDownObject.GetComponent<SplineAnimate>();
        hookUpSplineAnimate.Container = currentUpSpline;
        hookUpSplineAnimate.StartOffset = hookDownSplineAnimate.StartOffset;
        hookUpSplineAnimate.NormalizedTime = hookDownSplineAnimate.NormalizedTime;


        isHookDownActive = false;
        hookDownSprite.SetActive(false);
        hookDownRope.SetActive(false);
        currentDownSpline = null;

        HookUpdater();
    }

    void CheckGroundStatus()
    {

        var bounds = spriteRenderer.bounds;

        Vector2 locationForRaycastRight = (Vector2)transform.position + (Vector2.right * bounds.extents.x);
        Vector2 locationForRaycastLeft = (Vector2)transform.position + (Vector2.left * bounds.extents.x);

        var hitRight = Physics2D.Raycast(locationForRaycastRight, Vector2.down, groundCheckDistance, groundLayer);
        var hitLeft = Physics2D.Raycast(locationForRaycastLeft, Vector2.down, groundCheckDistance, groundLayer);


        if (hitRight.collider != null || hitLeft.collider != null && rb.IsTouchingLayers(groundLayer))
        {
            isGrounded = true;
            LogPlatformsTraversed(hitRight.collider != null ? hitRight.collider : hitLeft.collider);
        }
        else
        {
            isGrounded = false;
        }
    }

    void LogPlatformsTraversed(Collider2D platform)
    {
        // If the platform has not been traversed before, add it to the list and log it
        if (!traversedPlatforms.Contains(platform))
        {
            traversedPlatforms.Add(platform);
            Debug.Log($"New platform traversed: {platform.name}");
        } else
        {
            Debug.Log($"Already traversed platform: {platform.name}");
            traversedPlatforms.Remove(platform);
            traversedPlatforms.Add(platform);
        }

        // If the list exceeds 5 platforms, remove the oldest entry
        if (traversedPlatforms.Count > 5)
        {
            traversedPlatforms.RemoveAt(0);
        }
        Debug.Log($"Current traversed platforms: {string.Join(", ", traversedPlatforms.ConvertAll(p => p.name))}");

    }


    IEnumerator Die()
    {
        GetComponent<Collider2D>().enabled = false;
        spriteRenderer.flipY = true;
        rb.linearVelocity = new Vector2(0, 5);
        Debug.Log("Player has died.");

        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("KillZone"))
        {
            StartCoroutine(Die());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("KillZone"))
        {
            StartCoroutine(Die());
        }
    }

}
