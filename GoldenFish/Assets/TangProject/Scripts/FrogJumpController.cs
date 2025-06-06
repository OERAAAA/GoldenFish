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

    private Rigidbody playerRigidbody;
    private Vector3 lastHeadPosition;
    private Vector3 lastReferencePosition;
    private bool isGrounded = true;
    private bool hasJumped = false;
    private bool isInitialized = false;
    private bool isFirstJump = true;
    private bool isInLandDelay = false;

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (!playerRigidbody || !headTransform || !referenceObject)
        {

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



        if (relY > upwardVelocityThreshold)
        {
            PerformJump(verticalJumpForce, horizontalJumpForce);
            hasJumped = true;

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

            StartCoroutine(LandDelay());
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;

        }
    }

    private void CompleteInitialization()
    {
        isInitialized = true;
        hasJumped = false;

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

    }
}
