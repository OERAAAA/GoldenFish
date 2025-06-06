using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
public class ArmSwingMovement : MonoBehaviour
{
    [Header("XR References")]
    public XROrigin xrOrigin;
    public Transform head;

    [Header("Arm Swing Detection")]
    [Tooltip("�����ƶ�����С�ֱ��ٶ�")]
    public float minSwingSpeed = 1.0f;
    [Tooltip("�����ƶ�����С�ֱۼ��ٶ�")]
    public float minSwingAcceleration = 2.0f;
    [Tooltip("�ֱ۰ڶ�����ƽ��ϵ��")]
    [Range(0.1f, 0.9f)] public float smoothFactor = 0.5f;
    [Tooltip("�ֱ۰ڶ�����һ������ֵ")]
    [Range(0.1f, 0.9f)] public float directionConsistencyThreshold = 0.7f;
    [Tooltip("�ƶ���������С����֡��")]
    public int minContinuousFrames = 3;
    [Tooltip("�ڶ����������루��ͷ�����֣�")]
    public float maxArmDistance = 1.0f;

    [Header("Movement Settings")]
    [Tooltip("ÿһ���ľ��� (��)")]
    public float stepDistance = 0.5f;
    [Tooltip("ÿһ���ĳ���ʱ�� (��)")]
    public float stepDuration = 0.5f;
    [Tooltip("���벽֮��ļ��ʱ�� (��)")]
    public float stepCooldown = 0.3f;
    [Tooltip("�ƶ��ٶȳ���")]
    public float speedMultiplier = 1.0f;
    [Tooltip("����߶�ƫ�� (��ֹ����)")]
    public float groundHeightOffset = 0.1f;

    [Header("Step Animation")]
    [Tooltip("��������߶� (��)")]
    public float stepHeight = 0.1f;
    [Tooltip("�����׶�ռ������ʱ��ı���")]
    [Range(0.1f, 0.9f)] public float riseRatio = 0.4f;
    [Tooltip("�������")]
    public AnimationCurve stepCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Collision Settings")]
    [Tooltip("��ײ������ (��)")]
    public float collisionCheckDistance = 0.5f;
    [Tooltip("��ײ���뾶 (��)")]
    public float collisionRadius = 0.2f;
    [Tooltip("���߶�ƫ�� (��)")]
    public float collisionHeightOffset = 0.5f;

    [Header("FMOD")]
    [EventRef] public string footstep;

    [Header("Feedback Settings")]
    public bool enableHapticFeedback = true;
    [Range(0f, 1f)] public float hapticIntensity = 0.5f;
    public float hapticDuration = 0.1f;
    public GameObject swingIndicatorPrefab;

    private CharacterController characterController;
    private bool isMoving;
    private float lastStepTime;

    // ���ж�����ر���
    private Vector3 stepStartPosition;
    private Vector3 stepTargetPosition;
    private float stepProgress;
    private float currentVerticalOffset;
    private Vector3 moveDirection;

    // ���������
    private float groundYPosition;

    // ����������
    private Transform leftHand;
    private Transform rightHand;
    private XRController leftXRController;
    private XRController rightXRController;

    // �ֱ۰ڶ�������
    private Vector3 leftHandPrevPos;
    private Vector3 rightHandPrevPos;
    private Vector3 leftHandVelocity;
    private Vector3 rightHandVelocity;
    private Vector3 leftHandAcceleration;
    private Vector3 rightHandAcceleration;
    private float leftSwingMagnitude;
    private float rightSwingMagnitude;
    private int swingFrameCount;
    private GameObject leftSwingIndicator;
    private GameObject rightSwingIndicator;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // �Զ���ȡXR Origin
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();

        // �Զ���ȡͷ������
        if (head == null && xrOrigin != null)
            head = xrOrigin.Camera.transform;

        // ��ʼ������߶�
        groundYPosition = transform.position.y + groundHeightOffset;

        // �Զ���ȡ�����ֿ�����
        FindHandControllers();

        // �����ڶ�ָʾ��
        CreateSwingIndicators();
    }

    void FindHandControllers()
    {
        // �����ҵ������ֿ�����
        var controllers = FindObjectsOfType<XRController>();
        foreach (var controller in controllers)
        {
            if (controller.gameObject.name.Contains("Left") ||
                controller.gameObject.name.Contains("left") ||
                controller.gameObject.name.Contains("L_"))
            {
                leftHand = controller.transform;
                leftXRController = controller;
            }
            else if (controller.gameObject.name.Contains("Right") ||
                     controller.gameObject.name.Contains("right") ||
                     controller.gameObject.name.Contains("R_"))
            {
                rightHand = controller.transform;
                rightXRController = controller;
            }
        }

        // ���÷���������ͨ���������Ʋ���
        if (leftHand == null)
        {
            GameObject leftObj = GameObject.Find("LeftHand Controller");
            if (leftObj == null) leftObj = GameObject.Find("Left Controller");
            if (leftObj != null)
            {
                leftHand = leftObj.transform;
                leftXRController = leftObj.GetComponent<XRController>();
            }
        }

        if (rightHand == null)
        {
            GameObject rightObj = GameObject.Find("RightHand Controller");
            if (rightObj == null) rightObj = GameObject.Find("Right Controller");
            if (rightObj != null)
            {
                rightHand = rightObj.transform;
                rightXRController = rightObj.GetComponent<XRController>();
            }
        }

        // ��ʼ���ֱ�λ��
        if (leftHand != null) leftHandPrevPos = leftHand.position;
        if (rightHand != null) rightHandPrevPos = rightHand.position;

    }

    void CreateSwingIndicators()
    {
        if (swingIndicatorPrefab)
        {
            leftSwingIndicator = Instantiate(swingIndicatorPrefab);
            rightSwingIndicator = Instantiate(swingIndicatorPrefab);
            leftSwingIndicator.SetActive(false);
            rightSwingIndicator.SetActive(false);
        }

    }

    void Update()
    {
        // ȷ�����������ҵ�
        if (leftHand == null || rightHand == null)
        {
            // �������²��ҿ�����
            FindHandControllers();
            return;
        }

        // �����ֱ��˶�����
        UpdateArmMotionData();

        // ����ֱ۰ڶ�״̬
        bool isSwinging = CheckArmSwing();

        // ���°ڶ�ָʾ��
        UpdateSwingIndicators(isSwinging);

        // ����ֱ��ڰڶ��Ҳ����ƶ��л���ȴ�У���ʼ�ƶ�
        if (isSwinging && !isMoving && Time.time > lastStepTime + stepCooldown)
        {
            StartMovement();
            if (!string.IsNullOrEmpty(footstep))
                RuntimeManager.PlayOneShot(footstep);
        }
    }

    private void UpdateArmMotionData()
    {
        // ���������˶�����
        if (leftHand != null)
        {
            Vector3 currentLeftPos = leftHand.position;
            Vector3 newVelocity = (currentLeftPos - leftHandPrevPos) / Time.deltaTime;
            leftHandVelocity = Vector3.Lerp(leftHandVelocity, newVelocity, smoothFactor);
            leftHandAcceleration = (newVelocity - leftHandVelocity) / Time.deltaTime;
            leftHandPrevPos = currentLeftPos;

            // �����ֱ۰ڶ����ȣ���Ҫ����ǰ����
            if (head != null)
            {
                Vector3 headForward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
                leftSwingMagnitude = Vector3.Dot(leftHandVelocity, headForward);
            }
        }

        // ���������˶�����
        if (rightHand != null)
        {
            Vector3 currentRightPos = rightHand.position;
            Vector3 newVelocity = (currentRightPos - rightHandPrevPos) / Time.deltaTime;
            rightHandVelocity = Vector3.Lerp(rightHandVelocity, newVelocity, smoothFactor);
            rightHandAcceleration = (newVelocity - rightHandVelocity) / Time.deltaTime;
            rightHandPrevPos = currentRightPos;

            // �����ֱ۰ڶ����ȣ���Ҫ����ǰ����
            if (head != null)
            {
                Vector3 headForward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
                rightSwingMagnitude = Vector3.Dot(rightHandVelocity, headForward);
            }
        }
    }

    private bool CheckArmSwing()
    {
        // ���������Ƿ���Ч
        if (leftHand == null || rightHand == null || head == null) return false;

        // ����ֱ��Ƿ��ں���Χ�ڣ���ֹ���⣩
        float leftDistance = Vector3.Distance(head.position, leftHand.position);
        float rightDistance = Vector3.Distance(head.position, rightHand.position);
        if (leftDistance > maxArmDistance || rightDistance > maxArmDistance) return false;

        // ����ٶȺͼ��ٶ��Ƿ�ﵽ��ֵ
        bool leftSwinging = leftHandVelocity.magnitude > minSwingSpeed &&
                           leftHandAcceleration.magnitude > minSwingAcceleration;

        bool rightSwinging = rightHandVelocity.magnitude > minSwingSpeed &&
                            rightHandAcceleration.magnitude > minSwingAcceleration;

        // ���ڶ�����һ���ԣ�����ڶ���
        bool consistentDirection = Mathf.Sign(leftSwingMagnitude) != Mathf.Sign(rightSwingMagnitude) &&
                                  Mathf.Abs(leftSwingMagnitude) > directionConsistencyThreshold &&
                                  Mathf.Abs(rightSwingMagnitude) > directionConsistencyThreshold;

        // ��������ڶ�֡��
        if (leftSwinging && rightSwinging && consistentDirection)
        {
            swingFrameCount++;
        }
        else
        {
            swingFrameCount = Mathf.Max(0, swingFrameCount - 1); // �𽥼��ټ���
        }

        return swingFrameCount >= minContinuousFrames;
    }

    private void UpdateSwingIndicators(bool isSwinging)
    {
        if (!swingIndicatorPrefab) return;

        if (leftSwingIndicator && leftHand != null)
        {
            leftSwingIndicator.transform.position = leftHand.position;
            leftSwingIndicator.SetActive(isSwinging);

            // ���ݰڶ����ȵ���ָʾ����С
            float size = Mathf.Clamp(leftHandVelocity.magnitude / minSwingSpeed, 0.5f, 2f);
            leftSwingIndicator.transform.localScale = Vector3.one * size;
        }

        if (rightSwingIndicator && rightHand != null)
        {
            rightSwingIndicator.transform.position = rightHand.position;
            rightSwingIndicator.SetActive(isSwinging);

            // ���ݰڶ����ȵ���ָʾ����С
            float size = Mathf.Clamp(rightHandVelocity.magnitude / minSwingSpeed, 0.5f, 2f);
            rightSwingIndicator.transform.localScale = Vector3.one * size;
        }
    }

    private void StartMovement()
    {
        isMoving = true;
        lastStepTime = Time.time;
        stepProgress = 0f;
        currentVerticalOffset = 0f;
        swingFrameCount = 0; // ���ðڶ�֡��

        // �ƶ�����Ϊͷ�������ˮƽ����
        if (head != null)
        {
            moveDirection = new Vector3(head.forward.x, 0f, head.forward.z).normalized;
        }
        else
        {
            moveDirection = Vector3.forward;
        }

        // �����ֱ۰ڶ����ȵ����ƶ��ٶ�
        float averageSwing = (Mathf.Abs(leftSwingMagnitude) + Mathf.Abs(rightSwingMagnitude)) / 2f;
        float speed = stepDistance * speedMultiplier * Mathf.Clamp(averageSwing / minSwingSpeed, 0.5f, 2.0f);

        // �����ײ
        if (CheckCollision(moveDirection))
        {
            isMoving = false;
            return;
        }

        // ������ʼ��Ŀ��λ�ã�������ˮƽ�棩
        stepStartPosition = new Vector3(transform.position.x, groundYPosition, transform.position.z);
        stepTargetPosition = stepStartPosition + moveDirection * speed;

        // ��������
        if (enableHapticFeedback)
        {
            if (leftXRController != null)
                leftXRController.SendHapticImpulse(hapticIntensity, hapticDuration);
            if (rightXRController != null)
                rightXRController.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            UpdateMovement();
        }

        // ��ֹ����³���ǿ�Ʊ����ڵ���߶�
        if (!isMoving && Mathf.Abs(transform.position.y - groundYPosition) > 0.01f)
        {
            Vector3 correctedPosition = new Vector3(transform.position.x, groundYPosition, transform.position.z);
            characterController.Move(correctedPosition - transform.position);
        }
    }

    private void UpdateMovement()
    {
        // ���²�������
        stepProgress += Time.fixedDeltaTime / stepDuration;

        if (stepProgress >= 1f)
        {
            // ����ƶ� - ȷ��λ����ȷ
            Vector3 finalPosition = new Vector3(stepTargetPosition.x, groundYPosition, stepTargetPosition.z);
            characterController.Move(finalPosition - transform.position);

            isMoving = false;
            return;
        }

        // ����ˮƽλ�ã�ʹ��ƽ��������
        float horizontalProgress = Mathf.SmoothStep(0f, 1f, stepProgress);
        Vector3 horizontalPosition = Vector3.Lerp(stepStartPosition, stepTargetPosition, horizontalProgress);

        // ���㴹ֱƫ�ƣ�����������
        UpdateVerticalOffset();

        // �������λ�ã�ȷ��Y���ڵ���߶�+ƫ�ƣ�
        Vector3 finalPos = new Vector3(
            horizontalPosition.x,
            groundYPosition + currentVerticalOffset,
            horizontalPosition.z
        );

        // Ӧ���ƶ�
        characterController.Move(finalPos - transform.position);
    }

    private void UpdateVerticalOffset()
    {
        if (stepProgress < riseRatio)
        {
            // �����׶�
            float riseProgress = stepProgress / riseRatio;
            currentVerticalOffset = stepCurve.Evaluate(riseProgress) * stepHeight;
        }
        else
        {
            // �½��׶�
            float fallProgress = (stepProgress - riseRatio) / (1 - riseRatio);
            currentVerticalOffset = (1 - stepCurve.Evaluate(fallProgress)) * stepHeight;
        }
    }

    private bool CheckCollision(Vector3 direction)
    {
        // ִ������Ͷ������ײ����ˮƽ����
        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        Vector3 checkPosition = new Vector3(
            transform.position.x,
            transform.position.y + collisionHeightOffset,
            transform.position.z
        );

        if (Physics.SphereCast(
            checkPosition,
            collisionRadius,
            horizontalDirection,
            out RaycastHit hit,
            collisionCheckDistance))
        {
            // �����⵽��ײ�壬�Ҳ��Ǵ��������򷵻�����ײ
            if (hit.collider != null && !hit.collider.isTrigger)
            {

                return true;
            }
        }
        return false;
    }

    private void OnDestroy()
    {
        if (leftSwingIndicator) Destroy(leftSwingIndicator);
        if (rightSwingIndicator) Destroy(rightSwingIndicator);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // ������ײ��ⷶΧ
        Vector3 checkPosition = new Vector3(
            transform.position.x,
            transform.position.y + collisionHeightOffset,
            transform.position.z
        );

        Vector3 displayDirection = moveDirection != Vector3.zero ?
            new Vector3(moveDirection.x, 0f, moveDirection.z).normalized :
            (head != null ? new Vector3(head.forward.x, 0f, head.forward.z).normalized : Vector3.forward);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPosition, collisionRadius);
        Gizmos.DrawLine(checkPosition, checkPosition + displayDirection * collisionCheckDistance);

        // �����ƶ���Ϣ
        if (isMoving)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(stepStartPosition, 0.1f);
            Gizmos.DrawLine(stepStartPosition, stepTargetPosition);
            Gizmos.DrawSphere(stepTargetPosition, 0.1f);

            // ���Ƶ���߶�
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(stepStartPosition.x - 0.5f, groundYPosition, stepStartPosition.z),
                new Vector3(stepTargetPosition.x + 0.5f, groundYPosition, stepTargetPosition.z)
            );
        }

        // �����ֱ��ٶ�
        if (leftHand != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(leftHand.position,
                           leftHand.position + leftHandVelocity);
            Gizmos.DrawWireSphere(leftHand.position + leftHandVelocity, 0.05f);
        }

        if (rightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rightHand.position,
                           rightHand.position + rightHandVelocity);
            Gizmos.DrawWireSphere(rightHand.position + rightHandVelocity, 0.05f);
        }
    }
}