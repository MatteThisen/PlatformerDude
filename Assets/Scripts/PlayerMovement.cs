using System.Drawing;
using Unity.Collections;
using UnityEngine;
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

    private float splineEndCheckRadius = 0.05f;
    [SerializeField] private float splineStartCheckRadius;
    private bool isOnSpline = false;
    private bool hasReachedEnd = false;
    private float timeSinceSplineStart;
    private Vector2 splineVelocityCalcPoint1;
    private Vector2 splineVelocityCalcPoint2;
    public Vector2 splineExitVelocity;
    private float hookStartNormPosOnSpline;
    private int currentSplineIndex;

    [SerializeField] private SplineAnimate splineAnimate;
    private Rigidbody2D rb;
    private Collider2D splineCol;
    private float moveInput;
    private bool isGrounded;

    [SerializeField] private GameObject hookObject;
    private TargetJoint2D hookJoint;

    public AK.Wwise.Event hookAttachEvent;
    public AK.Wwise.Event hookDetachEvent;
    public AK.Wwise.RTPC cableValue;
    private float yValueToFreqMultiplier = 100f;

    private SplineContainer[] allSplines;
    private SplineContainer currentSpline;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hookJoint = GetComponent<TargetJoint2D>();
        splineVelocityCalcPoint1 = transform.position;

        allSplines = FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);

    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Debug.Log("Jump");
            Jump();
        }

        // check if player is near enough to spline and if the player presses space, connect to spline
        if (Input.GetKeyDown(KeyCode.Space))
        {
          
            ConnectToSpline();
            /* splineCol = Physics2D.OverlapCircle(transform.position, groundCheckRadius, 1 << LayerMask.NameToLayer("SplineBox"));

            if (splineCol != null)
            {
                ConnectToSpline();
            }*/
        }

        // If the player is on a spline, update the hook position and check for disconnect conditions
        if (isOnSpline)
        {
            timeSinceSplineStart += Time.deltaTime;

            hasReachedEnd = Physics2D.OverlapCircle(hookObject.transform.position, splineEndCheckRadius, 1 << LayerMask.NameToLayer("SplineEndPoint"));

            rb.gravityScale = 0;

            Vector2 playerVelocity = rb.linearVelocity;

            HookUpdater();

            // this if statement disconnects the player from the spline if true
            if (Input.GetKeyDown(KeyCode.Space) && timeSinceSplineStart > 0.2f || hasReachedEnd)
            {
                DisconnectFromSpline();
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
        if (moveInput != 0)
        {
            float targetSpeed = moveInput * maxSpeed;
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

    void Jump()
    {
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    void DisconnectFromSpline()
    {
        splineAnimate.Pause();
        splineAnimate.enabled = false;
        Debug.Log("Disconnected from SplineBox");

        hookDetachEvent.Post(gameObject);
        hookObject.SetActive(false);
        hookJoint.enabled = false;
        isOnSpline = false;
        timeSinceSplineStart = 0;
        hasReachedEnd = false;
        rb.gravityScale = 1;
        rb.linearVelocity = splineExitVelocity;
    }

    void ConnectToSpline()
    {

        if (!FindClosestSplinePoint())
        {
            Debug.Log("No spline found within range");
            return;
        }

        isOnSpline = true;
        splineAnimate.StartOffset = hookStartNormPosOnSpline;
        splineAnimate.Container = allSplines[currentSplineIndex];
        splineAnimate.enabled = true;
        hookObject.SetActive(true);
        hookAttachEvent.Post(gameObject);

        splineAnimate.Restart(false);
        splineAnimate.Play();
        Debug.Log("Collided with SplineBox");

        hookJoint.enabled = true;
    }

    void HookUpdater()
    {
        if (hookJoint != null)
        {
            // t finds the normalized time along the spline, as well as the offset from the start of the spline found in FindClosestSplinePoint
            float t = splineAnimate.NormalizedTime + hookStartNormPosOnSpline;

            // Get the position along the spline
            Vector3 positionOnSpline = currentSpline.Spline.EvaluatePosition(t);

            Vector3 targetPosition = positionOnSpline + currentSpline.GetComponentInParent<Transform>().position;

            SetCableRTPC(targetPosition);

            hookJoint.target = new Vector2(targetPosition.x, targetPosition.y);
        }
    }

    bool FindClosestSplinePoint()
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

        return isSplineWithinRange; // Return the t value of the closest point found
    }
    public void SetCableRTPC(Vector3 hookPosition)
    {
        float yValue = hookPosition.y;

        float rtpcValue = yValue * yValueToFreqMultiplier;

        cableValue.SetGlobalValue(rtpcValue);
    }
}
