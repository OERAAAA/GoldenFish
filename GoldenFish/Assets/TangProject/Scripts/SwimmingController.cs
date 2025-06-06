using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

public class SwimmingController : MonoBehaviour
{
    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;
    public Transform playerCamera;

    [Header("Physics Settings")]
    public float upForce = 0f;
    public float gravity = 0f;
    public float maxSpeed = 6f;
    public float forceMagnitude = 2.7f;

    [Header("Flap Forces")]
    public float lightFlapForce = 5f;
    public float strongFlapForce = 9f;

    [Header("Swing Detection")]
    public float xRotationThreshold = 5f;
    public float strongFlapThreshold = 20f;
    public float swingDelay = 0.5f;

    [Header("Activation List")]
    public List<GameObject> thingsToActivate; // 👈 新增：要启用的对象列表
    public float startFalling;

    private Rigidbody playerRigidbody;
    private float lastSwingTime;
    private Quaternion lastLeftRotation;
    private Quaternion lastRightRotation;
    private Vector3 flapForceDirection;
    private Vector3 upWard = new Vector3(0f, 2f, 0f).normalized;

    private bool hasFirstFlapOccurred = false;
    private int flapCount = 0;                 // 👈 新增：统计 flap 次数
    private bool hasActivatedList = false;     // 👈 新增：只激活一次

    [EventRef] public string catchSoundEvent;

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        if (!playerRigidbody || !leftController || !rightController || !playerCamera)
        {

            enabled = false;
            return;
        }

        lastLeftRotation = leftController.rotation;
        lastRightRotation = rightController.rotation;
    }

    private void Update()
    {
        flapForceDirection = playerCamera.forward;
        DetectSwing();
    }

    private void FixedUpdate()
    {
        playerRigidbody.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        // Apply drag and limit speed
        Vector3 velocity = playerRigidbody.velocity;
        playerRigidbody.AddForce(-velocity.normalized * forceMagnitude, ForceMode.Force);
        if (velocity.magnitude > maxSpeed)
        {
            playerRigidbody.velocity = velocity.normalized * maxSpeed;
        }
    }

    private void DetectSwing()
    {
        float leftRotationDelta = Quaternion.Angle(lastLeftRotation, leftController.rotation);
        float rightRotationDelta = Quaternion.Angle(lastRightRotation, rightController.rotation);

        if (leftRotationDelta > xRotationThreshold && rightRotationDelta > xRotationThreshold && Time.time - lastSwingTime >= swingDelay)
        {
            if (!hasFirstFlapOccurred)
            {
                hasFirstFlapOccurred = true;
            }
            else
            {
                // Determine flap strength
                if (leftRotationDelta > strongFlapThreshold && rightRotationDelta > strongFlapThreshold)
                {
                    Flap(strongFlapForce);
                }
                else
                {
                    Flap(lightFlapForce);
                }

                RuntimeManager.PlayOneShot(catchSoundEvent);

                flapCount++; // 👈 增加 flap 次数

                // 👇 达到三次后激活列表中的物体，只执行一次
                if (flapCount >= startFalling && !hasActivatedList)
                {
                    hasActivatedList = true;
                    ActivateThings();
                }
            }

            lastSwingTime = Time.time;
        }

        lastLeftRotation = leftController.rotation;
        lastRightRotation = rightController.rotation;
    }

    private void Flap(float flapPower)
    {
        playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, 0.8f, playerRigidbody.velocity.z);
        playerRigidbody.AddForce(flapForceDirection * flapPower + upWard * upForce, ForceMode.VelocityChange);
    }

    private void ActivateThings()
    {
        foreach (var obj in thingsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    public void TriggerFlap()
    {
        Flap(lightFlapForce);
    }

    public void ResetFirstFlap()
    {
        hasFirstFlapOccurred = false;
        flapCount = 0;
        hasActivatedList = false;
    }
}
