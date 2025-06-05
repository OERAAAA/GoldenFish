using UnityEngine;
using System.Collections;

public class FrogJumpController : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    public Transform referenceObject;
    public LayerMask groundLayer;

    [Header("Jump Settings")]
    public float upwardVelocityThreshold = 1.2f;
    public float verticalJumpForce = 6.0f;
    public float horizontalJumpForce = 2.0f;
    public float gravity = -9.81f;
    public float landDelayTime = 0.5f;

    [Header("Mini Jump Settings")]
    public float miniJumpVelocityThreshold = 0.5f;
    public float miniJumpForce = 2.5f;
    public float miniHorizontalForce = 1.0f;

    [Header("Advanced Jump Timing")]
    public float jumpBufferTime = 0.1f;

    [Header("Improved Crouch Detection")]
    public float crouchDepthThreshold = 0.25f;   // 下蹲 Y 轴位移
    public float crouchCooldownTime = 1.0f;       // 下蹲有效时间

    private Rigidbody playerRigidbody;
    private Vector3 lastHeadPosition;
    private Vector3 lastReferencePosition;
    private bool isGrounded = true;
    private bool hasJumped = false;
    private bool isInitialized = false;
    private bool isFirstJump = true;
    private bool isInLandDelay = false;

    private float jumpBufferTimer = 0f;
    private bool wantsBigJump = false;

    // 下蹲状态
    private bool wasCrouched = false;
    private float crouchTimer = 0f;

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (!playerRigidbody || !headTransform || !referenceObject)
        {
            Debug.LogError("FrogJumpController requires headTransform, referenceObject and Rigidbody!");
            enabled = false;
            return;
        }

        lastHeadPosition = headTransform.position;
        lastReferencePosition = referenceObject.position;

        StartCoroutine(InitializeAfterDelay(1f));
    }

    private void Update()
    {
        if (!isInitialized) return;

        DetectCrouch();
        DetectHeadJump();

        lastHeadPosition = headTransform.position;
        lastReferencePosition = referenceObject.position;
    }

    private void FixedUpdate()
    {
        if (!isGrounded)
        {
            playerRigidbody.velocity += new Vector3(0, gravity * Time.deltaTime, 0);
        }
    }

    private void DetectCrouch()
    {
        float deltaY = lastHeadPosition.y - headTransform.position.y;

        if (!wasCrouched && deltaY > crouchDepthThreshold)
        {
            wasCrouched = true;
            crouchTimer = crouchCooldownTime;
            Debug.Log("Crouch Detected");
        }

        if (wasCrouched)
        {
            crouchTimer -= Time.deltaTime;
            if (crouchTimer <= 0f)
            {
                wasCrouched = false;
            }
        }
    }

    private void DetectHeadJump()
    {
        if (isInLandDelay || !isGrounded || !isInitialized || hasJumped)
            return;

        if (isFirstJump)
        {
            isFirstJump = false;
            return;
        }

        Vector3 velocityHead = (headTransform.position - lastHeadPosition) / Time.deltaTime;
        Vector3 velocityReference = (referenceObject.position - lastReferencePosition) / Time.deltaTime;
        Vector3 relativeVelocity = velocityHead - velocityReference;
        float relY = relativeVelocity.y;

        Debug.Log($"Relative Y Velocity: {relY:F3}");

        // 大跳必须是下蹲后再快速抬头
        if (wasCrouched && relY > upwardVelocityThreshold)
        {
            PerformJump(verticalJumpForce, horizontalJumpForce);
            hasJumped = true;
            wasCrouched = false;
            crouchTimer = 0f;
            Debug.Log("Improved Big Jump Triggered!");
        }
        // 小跳不要求下蹲
        else if (relY > miniJumpVelocityThreshold)
        {
            PerformJump(miniJumpForce, miniHorizontalForce);
            hasJumped = true;
            wasCrouched = false;
            Debug.Log("Mini Jump Triggered!");
        }
    }

    private void PerformJump(float jumpForce, float horizontalForce)
    {
        playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, jumpForce, playerRigidbody.velocity.z);

        Vector3 jumpDirection = headTransform.forward;
        jumpDirection.y = 0;

        if (jumpDirection.magnitude > 0.1f)
        {
            jumpDirection.Normalize();
            playerRigidbody.AddForce(jumpDirection * horizontalForce, ForceMode.VelocityChange);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
            hasJumped = false;
            wantsBigJump = false;
            jumpBufferTimer = 0f;
            wasCrouched = false;
            Debug.Log("Grounded");

            StartCoroutine(LandDelay());
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
            Debug.Log("Left Ground");
        }
    }

    private void CompleteInitialization()
    {
        isInitialized = true;
        hasJumped = false;
        Debug.Log("Initialization Complete.");
    }

    private IEnumerator InitializeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteInitialization();
    }

    private IEnumerator LandDelay()
    {
        isInLandDelay = true;
        yield return new WaitForSeconds(landDelayTime);
        isInLandDelay = false;
        Debug.Log("Land delay over. Ready for next jump detection.");
    }
}
