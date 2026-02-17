using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Transactions;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using static UnityEngine.GraphicsBuffer;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 5f;
    public float jumpForce = 10f;

    public LayerMask groundLayer;
    public float groundCheckRadius = 5f;
    [SerializeField] private float splineStartCheckRadius;
    private bool isOnSpline = false;
    private bool hasReachedEnd = false;
    private float timeSinceSplineStart;
    private Vector2 splineVelocityCalcPoint1;
    private Vector2 splineVelocityCalcPoint2;
    public Vector2 splineExitVelocity;
    private float hookStartNormPosOnSpline;
    private int currentSplineIndex;

    private enum HookState { None, Up, UpAndDown }
    private HookState currentHookState;

    private SplineAnimate splineAnimateUp;
    private SplineAnimate splineAnimateDown;
    private Rigidbody2D rb;
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
    private SplineContainer currentSpline;
    private SplineBoxBehaviour splineBoxBehaviour;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        hookJoint = GetComponent<TargetJoint2D>();
        
        
        
        /*hookSprite = hookObject.GetComponentInChildren<SpriteRenderer>().gameObject;
        hookRope = hookObject.GetComponentInChildren<LineRenderer>().gameObject;

        if (hookObject == null || hookSprite == null || hookRope == null)
        {
            Debug.LogError("Hook object and/or children not found.");
        } else {
            hookObject.SetActive(false);
            hookSprite.SetActive(false);
            hookRope.SetActive(false);
        }*/


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
        isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);

        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();

        // If the player is on a spline, update the hook position and check for disconnect conditions
        if (currentHookState != HookState.None)
        {
            timeSinceSplineStart += Time.deltaTime;

            rb.gravityScale = 0;

            Vector2 playerVelocity = rb.linearVelocity;

            HookUpdater();

        }

        if (playerInput.actions["HookUp"].WasPressedThisFrame())
        {
            if (currentHookState == HookState.None)
            {
                ConnectToSpline(hookUpObject);
            }
            else if (currentHookState == HookState.Up)
            {
                DisconnectFromSpline(hookUpObject);
            }
            else if (currentHookState == HookState.UpAndDown)
            {
                //DisconnectOnlyUpHook(hookUpObject);
            }
        }

        if (playerInput.actions["HookDown"].WasPressedThisFrame() && currentHookState != HookState.None)
        {
            if (currentHookState == HookState.Up)
            {
                ConnectToSpline(hookDownObject);
            }
            else if (currentHookState == HookState.UpAndDown)
            {
                DisconnectFromSpline(hookDownObject);
            }
        }
    }
    void FixedUpdate()
    {
        ApplyMovement();

        splineVelocityCalcPoint1 = splineVelocityCalcPoint2;
        splineVelocityCalcPoint2 = transform.position;

        if (isOnSpline)
        {
            Vector2 distanceTravelled = splineVelocityCalcPoint2 - splineVelocityCalcPoint1;
            splineExitVelocity = distanceTravelled / Time.fixedDeltaTime;
        }

    }

    void ApplyMovement()
    {
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
        if (hookObject == hookUpObject)
        {
            currentHookState = HookState.None;
            isHookUpActive = false;
            hookUpSprite.SetActive(false);
            hookUpRope.SetActive(false);
        } else
        {
            currentHookState = HookState.Up;
            isHookDownActive = false;
            hookDownSprite.SetActive(false);
            hookDownRope.SetActive(false);
        }



        SplineAnimate splineAnimate = hookObject.GetComponent<SplineAnimate>();

        splineAnimate.Pause();
        splineAnimate.enabled = false;
        Debug.Log("Disconnected from SplineBox");

        if (splineBoxBehaviour != null)
        {
            splineBoxBehaviour.StopNote();
        }
        splineBoxBehaviour = null;

        hookObject.SetActive(false);

        hookJoint.enabled = false;
        isOnSpline = false;
        timeSinceSplineStart = 0;
        hasReachedEnd = false;
        rb.gravityScale = 1;
        rb.linearVelocity = splineExitVelocity;
        currentSpline = null;
    }

    void ConnectToSpline(GameObject hookObject)
    {

        if (!FindClosestSplinePoint(hookObject))
        {
            Debug.Log("No spline found within range");
            return;
        }


        SplineAnimate splineAnimate = hookObject.GetComponent<SplineAnimate>();

        isOnSpline = true;
        splineAnimate.StartOffset = hookStartNormPosOnSpline;
        splineAnimate.Container = allSplines[currentSplineIndex];
        splineAnimate.enabled = true;
        hookObject.SetActive(true);

        splineBoxBehaviour = currentSpline.GetComponentInParent<SplineBoxBehaviour>();
        if (splineBoxBehaviour != null) {
            splineBoxBehaviour.PlayNote();
        }

        splineAnimate.Restart(false);
        splineAnimate.Play();
        Debug.Log("Collided with SplineBox");

        hookJoint.enabled = true;

        if (hookObject == hookUpObject)
        {
            currentHookState = HookState.Up;
            isHookUpActive = true;
            hookUpRope.SetActive(true);
            hookUpSprite.SetActive(true);
        } else
        {
            currentHookState = HookState.UpAndDown;
            isHookDownActive = true;
            hookDownRope.SetActive(true);
            hookDownSprite.SetActive(true);
        }
    }

    void HookUpdater()
    {
        if (hookJoint != null || isHookUpActive)
        {
          
            SplineAnimate splineAnimate = hookUpObject.GetComponent<SplineAnimate>();
            // t finds the normalized time along the spline, as well as the offset from the start of the spline found in FindClosestSplinePoint
            float t = splineAnimate.NormalizedTime + hookStartNormPosOnSpline;

            if (t > 0.99f)
            {
                hasReachedEnd = true;
            }

            // Get the position along the spline
            Vector3 positionOnSpline = currentSpline.Spline.EvaluatePosition(t);

            Vector3 targetPosition = positionOnSpline + currentSpline.GetComponentInParent<Transform>().position;

            hookJoint.target = new Vector2(targetPosition.x, targetPosition.y);

        }

        if (isHookDownActive)
        {
            SplineAnimate splineAnimate = hookDownObject.GetComponent<SplineAnimate>();
            // t finds the normalized time along the spline, as well as the offset from the start of the spline found in FindClosestSplinePoint
            float t = splineAnimate.NormalizedTime + hookStartNormPosOnSpline;

            if (t > 0.99f)
            {
                hasReachedEnd = true;
            }
        }
    }

    bool FindClosestSplinePoint(GameObject hookObject)
    {
        bool isSplineWithinRange = false;
        float minDist = float.MaxValue;
        SplineContainer closestContainer = null;
        hookStartNormPosOnSpline = 0f;

        foreach (var container in allSplines)
        {
            var spline = container.Spline;

            // Sample spline at multiple points to approximate closest point
            int samples = 50;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3 point = container.EvaluatePosition(t);
                float dist = Vector2.Distance(transform.position, point);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestContainer = container;
                    hookStartNormPosOnSpline = t;
                }
            }
        }

        Vector3 pointOfEntry = closestContainer.EvaluatePosition(hookStartNormPosOnSpline);

        if (closestContainer != null && Vector2.Distance(transform.position, pointOfEntry) < splineStartCheckRadius)
        {
            Debug.Log($"Closest spline found at t={hookStartNormPosOnSpline}, distance={minDist}");
            currentSpline = closestContainer;
            currentSplineIndex = System.Array.IndexOf(allSplines, closestContainer);
            isSplineWithinRange = true;
        } else
        {
            isSplineWithinRange = false;
            Debug.Log("No spline found within range");
        }

        return isSplineWithinRange; // Return whether a spline was found within range
    }
}
