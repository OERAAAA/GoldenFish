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
    [Tooltip("触发移动的最小手臂速度")]
    public float minSwingSpeed = 1.0f;
    [Tooltip("触发移动的最小手臂加速度")]
    public float minSwingAcceleration = 2.0f;
    [Tooltip("手臂摆动检测的平滑系数")]
    [Range(0.1f, 0.9f)] public float smoothFactor = 0.5f;
    [Tooltip("手臂摆动方向一致性阈值")]
    [Range(0.1f, 0.9f)] public float directionConsistencyThreshold = 0.7f;
    [Tooltip("移动触发的最小连续帧数")]
    public int minContinuousFrames = 3;
    [Tooltip("摆动检测的最大距离（从头部到手）")]
    public float maxArmDistance = 1.0f;

    [Header("Movement Settings")]
    [Tooltip("每一步的距离 (米)")]
    public float stepDistance = 0.5f;
    [Tooltip("每一步的持续时间 (秒)")]
    public float stepDuration = 0.5f;
    [Tooltip("步与步之间的间隔时间 (秒)")]
    public float stepCooldown = 0.3f;
    [Tooltip("移动速度乘数")]
    public float speedMultiplier = 1.0f;
    [Tooltip("地面高度偏移 (防止穿地)")]
    public float groundHeightOffset = 0.1f;

    [Header("Step Animation")]
    [Tooltip("步伐起伏高度 (米)")]
    public float stepHeight = 0.1f;
    [Tooltip("上升阶段占步持续时间的比例")]
    [Range(0.1f, 0.9f)] public float riseRatio = 0.4f;
    [Tooltip("起伏曲线")]
    public AnimationCurve stepCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Collision Settings")]
    [Tooltip("碰撞检测距离 (米)")]
    public float collisionCheckDistance = 0.5f;
    [Tooltip("碰撞检测半径 (米)")]
    public float collisionRadius = 0.2f;
    [Tooltip("检测高度偏移 (米)")]
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

    // 步行动画相关变量
    private Vector3 stepStartPosition;
    private Vector3 stepTargetPosition;
    private float stepProgress;
    private float currentVerticalOffset;
    private Vector3 moveDirection;

    // 地面检测相关
    private float groundYPosition;

    // 控制器引用
    private Transform leftHand;
    private Transform rightHand;
    private XRController leftXRController;
    private XRController rightXRController;

    // 手臂摆动检测相关
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

        // 自动获取XR Origin
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();

        // 自动获取头部引用
        if (head == null && xrOrigin != null)
            head = xrOrigin.Camera.transform;

        // 初始化地面高度
        groundYPosition = transform.position.y + groundHeightOffset;

        // 自动获取左右手控制器
        FindHandControllers();

        // 创建摆动指示器
        CreateSwingIndicators();
    }

    void FindHandControllers()
    {
        // 尝试找到左右手控制器
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

        // 备用方案：尝试通过常见名称查找
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

        // 初始化手臂位置
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
        // 确保控制器已找到
        if (leftHand == null || rightHand == null)
        {
            // 尝试重新查找控制器
            FindHandControllers();
            return;
        }

        // 更新手臂运动数据
        UpdateArmMotionData();

        // 检测手臂摆动状态
        bool isSwinging = CheckArmSwing();

        // 更新摆动指示器
        UpdateSwingIndicators(isSwinging);

        // 如果手臂在摆动且不在移动中或冷却中，开始移动
        if (isSwinging && !isMoving && Time.time > lastStepTime + stepCooldown)
        {
            StartMovement();
            if (!string.IsNullOrEmpty(footstep))
                RuntimeManager.PlayOneShot(footstep);
        }
    }

    private void UpdateArmMotionData()
    {
        // 更新左手运动数据
        if (leftHand != null)
        {
            Vector3 currentLeftPos = leftHand.position;
            Vector3 newVelocity = (currentLeftPos - leftHandPrevPos) / Time.deltaTime;
            leftHandVelocity = Vector3.Lerp(leftHandVelocity, newVelocity, smoothFactor);
            leftHandAcceleration = (newVelocity - leftHandVelocity) / Time.deltaTime;
            leftHandPrevPos = currentLeftPos;

            // 计算手臂摆动幅度（主要考虑前后方向）
            if (head != null)
            {
                Vector3 headForward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
                leftSwingMagnitude = Vector3.Dot(leftHandVelocity, headForward);
            }
        }

        // 更新右手运动数据
        if (rightHand != null)
        {
            Vector3 currentRightPos = rightHand.position;
            Vector3 newVelocity = (currentRightPos - rightHandPrevPos) / Time.deltaTime;
            rightHandVelocity = Vector3.Lerp(rightHandVelocity, newVelocity, smoothFactor);
            rightHandAcceleration = (newVelocity - rightHandVelocity) / Time.deltaTime;
            rightHandPrevPos = currentRightPos;

            // 计算手臂摆动幅度（主要考虑前后方向）
            if (head != null)
            {
                Vector3 headForward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
                rightSwingMagnitude = Vector3.Dot(rightHandVelocity, headForward);
            }
        }
    }

    private bool CheckArmSwing()
    {
        // 检查控制器是否有效
        if (leftHand == null || rightHand == null || head == null) return false;

        // 检查手臂是否在合理范围内（防止误检测）
        float leftDistance = Vector3.Distance(head.position, leftHand.position);
        float rightDistance = Vector3.Distance(head.position, rightHand.position);
        if (leftDistance > maxArmDistance || rightDistance > maxArmDistance) return false;

        // 检查速度和加速度是否达到阈值
        bool leftSwinging = leftHandVelocity.magnitude > minSwingSpeed &&
                           leftHandAcceleration.magnitude > minSwingAcceleration;

        bool rightSwinging = rightHandVelocity.magnitude > minSwingSpeed &&
                            rightHandAcceleration.magnitude > minSwingAcceleration;

        // 检查摆动方向一致性（交替摆动）
        bool consistentDirection = Mathf.Sign(leftSwingMagnitude) != Mathf.Sign(rightSwingMagnitude) &&
                                  Mathf.Abs(leftSwingMagnitude) > directionConsistencyThreshold &&
                                  Mathf.Abs(rightSwingMagnitude) > directionConsistencyThreshold;

        // 检查连续摆动帧数
        if (leftSwinging && rightSwinging && consistentDirection)
        {
            swingFrameCount++;
        }
        else
        {
            swingFrameCount = Mathf.Max(0, swingFrameCount - 1); // 逐渐减少计数
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

            // 根据摆动幅度调整指示器大小
            float size = Mathf.Clamp(leftHandVelocity.magnitude / minSwingSpeed, 0.5f, 2f);
            leftSwingIndicator.transform.localScale = Vector3.one * size;
        }

        if (rightSwingIndicator && rightHand != null)
        {
            rightSwingIndicator.transform.position = rightHand.position;
            rightSwingIndicator.SetActive(isSwinging);

            // 根据摆动幅度调整指示器大小
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
        swingFrameCount = 0; // 重置摆动帧数

        // 移动方向为头部朝向的水平方向
        if (head != null)
        {
            moveDirection = new Vector3(head.forward.x, 0f, head.forward.z).normalized;
        }
        else
        {
            moveDirection = Vector3.forward;
        }

        // 根据手臂摆动幅度调整移动速度
        float averageSwing = (Mathf.Abs(leftSwingMagnitude) + Mathf.Abs(rightSwingMagnitude)) / 2f;
        float speed = stepDistance * speedMultiplier * Mathf.Clamp(averageSwing / minSwingSpeed, 0.5f, 2.0f);

        // 检查碰撞
        if (CheckCollision(moveDirection))
        {
            isMoving = false;
            return;
        }

        // 设置起始和目标位置（限制在水平面）
        stepStartPosition = new Vector3(transform.position.x, groundYPosition, transform.position.z);
        stepTargetPosition = stepStartPosition + moveDirection * speed;

        // 触觉反馈
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

        // 防止玩家下沉：强制保持在地面高度
        if (!isMoving && Mathf.Abs(transform.position.y - groundYPosition) > 0.01f)
        {
            Vector3 correctedPosition = new Vector3(transform.position.x, groundYPosition, transform.position.z);
            characterController.Move(correctedPosition - transform.position);
        }
    }

    private void UpdateMovement()
    {
        // 更新步伐进度
        stepProgress += Time.fixedDeltaTime / stepDuration;

        if (stepProgress >= 1f)
        {
            // 完成移动 - 确保位置正确
            Vector3 finalPosition = new Vector3(stepTargetPosition.x, groundYPosition, stepTargetPosition.z);
            characterController.Move(finalPosition - transform.position);

            isMoving = false;
            return;
        }

        // 计算水平位置（使用平滑步进）
        float horizontalProgress = Mathf.SmoothStep(0f, 1f, stepProgress);
        Vector3 horizontalPosition = Vector3.Lerp(stepStartPosition, stepTargetPosition, horizontalProgress);

        // 计算垂直偏移（步伐动画）
        UpdateVerticalOffset();

        // 组合最终位置（确保Y轴在地面高度+偏移）
        Vector3 finalPos = new Vector3(
            horizontalPosition.x,
            groundYPosition + currentVerticalOffset,
            horizontalPosition.z
        );

        // 应用移动
        characterController.Move(finalPos - transform.position);
    }

    private void UpdateVerticalOffset()
    {
        if (stepProgress < riseRatio)
        {
            // 上升阶段
            float riseProgress = stepProgress / riseRatio;
            currentVerticalOffset = stepCurve.Evaluate(riseProgress) * stepHeight;
        }
        else
        {
            // 下降阶段
            float fallProgress = (stepProgress - riseRatio) / (1 - riseRatio);
            currentVerticalOffset = (1 - stepCurve.Evaluate(fallProgress)) * stepHeight;
        }
    }

    private bool CheckCollision(Vector3 direction)
    {
        // 执行球体投射检测碰撞（仅水平方向）
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
            // 如果检测到碰撞体，且不是触发器，则返回有碰撞
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

        // 绘制碰撞检测范围
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

        // 绘制移动信息
        if (isMoving)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(stepStartPosition, 0.1f);
            Gizmos.DrawLine(stepStartPosition, stepTargetPosition);
            Gizmos.DrawSphere(stepTargetPosition, 0.1f);

            // 绘制地面高度
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(stepStartPosition.x - 0.5f, groundYPosition, stepStartPosition.z),
                new Vector3(stepTargetPosition.x + 0.5f, groundYPosition, stepTargetPosition.z)
            );
        }

        // 绘制手臂速度
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