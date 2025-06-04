using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
public class VRJoystickMovement : MonoBehaviour
{
    [Header("VR References")]
    public XROrigin xrOrigin;
    public Transform head; // ͷ��λ�ã�ͨ����XR Origin�µ�Main Camera��

    [Header("Movement Settings")]
    [Tooltip("�ƶ��ٶ� (��/��)")]
    public float moveSpeed = 1.5f;
    [Tooltip("ÿһ���ľ��� (��)")]
    public float stepDistance = 0.5f;
    [Tooltip("ÿһ���ĳ���ʱ�� (��)")]
    public float stepDuration = 0.4f;
    [Tooltip("���벽֮��ļ��ʱ�� (��)")]
    public float stepCooldown = 0.1f;
    [Tooltip("ҡ���ƶ���ֵ (0-1)")]
    [Range(0.1f, 0.9f)] public float joystickThreshold = 0.3f;

    [Header("Step Animation")]
    [Tooltip("��������߶� (��)")]
    public float stepHeight = 0.05f;
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

    private CharacterController characterController;
    private Vector2 joystickInput;
    private bool isMoving;
    private float lastStepTime;
    private Vector3 moveDirection;
    private bool canMove = true;
    private InputDevice leftHandDevice;

    // ���ж�����ر���
    private Vector3 stepStartPosition;  // ������ʼλ�ã�ˮƽ��
    private Vector3 stepTargetPosition; // ����Ŀ��λ�ã�ˮƽ��
    private float stepProgress;         // �������� (0-1)
    private float currentVerticalOffset; // ��ǰ��ֱƫ����

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();

        if (head == null && xrOrigin != null)
            head = xrOrigin.Camera.transform;

        // ��ʼ�������豸
        InitializeLeftHandDevice();
    }

    void InitializeLeftHandDevice()
    {
        // ���Ի�ȡ�����豸
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);

        if (leftHandDevices.Count > 0)
        {
            leftHandDevice = leftHandDevices[0];
            Debug.Log("�ҵ������豸: " + leftHandDevice.name);
        }
        else
        {
            Debug.LogWarning("δ�ҵ������豸��");
        }
    }

    void Update()
    {
        if (!canMove) return;

        // ���û����Ч�������豸���������³�ʼ��
        if (!leftHandDevice.isValid)
        {
            InitializeLeftHandDevice();
            return;
        }

        // ��ȡ�����ֱ�ҡ������
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInput))
        {
            // ����Ƿ����ƶ��л���ȴ��
            bool inCooldown = Time.time < lastStepTime + stepCooldown;

            // �����ҡ�������Ҳ����ƶ��л���ȴ�У���ʼ�ƶ�
            if (joystickInput.magnitude > joystickThreshold && !isMoving && !inCooldown)
            {
                StartMovement();
            }
        }

        // �����ƶ��Ͷ���
        if (isMoving)
        {
            UpdateMovement();
        }
    }

    private void StartMovement()
    {
        isMoving = true;
        lastStepTime = Time.time;
        stepProgress = 0f;
        currentVerticalOffset = 0f;

        // �����ƶ����򣨻���ͷ������
        Vector3 horizontalDirection = new Vector3(head.forward.x, 0f, head.forward.z).normalized;
        Vector3 rightDirection = new Vector3(head.right.x, 0f, head.right.z).normalized;

        // ����ҡ������ȷ���ƶ�����
        moveDirection = (horizontalDirection * joystickInput.y + rightDirection * joystickInput.x).normalized;

        // �����ײ
        if (CheckCollision(moveDirection))
        {
            // �������ײ��ȡ���ƶ�
            isMoving = false;
            return;
        }

        // ������ʼ��Ŀ��λ��
        stepStartPosition = transform.position;
        stepTargetPosition = stepStartPosition + moveDirection * stepDistance;
    }

    private void UpdateMovement()
    {
        // ���²�������
        stepProgress += Time.deltaTime / stepDuration;

        if (stepProgress >= 1f)
        {
            // ����ƶ�
            transform.position = stepTargetPosition;
            isMoving = false;
            return;
        }

        // ����ˮƽλ�ã�ʹ��ƽ��������
        float horizontalProgress = Mathf.SmoothStep(0f, 1f, stepProgress);
        Vector3 horizontalPosition = Vector3.Lerp(stepStartPosition, stepTargetPosition, horizontalProgress);

        // ���㴹ֱƫ�ƣ�����������
        UpdateVerticalOffset();

        // �������λ��
        Vector3 finalPosition = horizontalPosition + Vector3.up * currentVerticalOffset;

        // Ӧ���ƶ�
        characterController.Move(finalPosition - transform.position);
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
        // ִ������Ͷ������ײ
        Vector3 checkPosition = transform.position + Vector3.up * collisionHeightOffset;

        if (Physics.SphereCast(
            checkPosition,
            collisionRadius,
            direction,
            out RaycastHit hit,
            collisionCheckDistance))
        {
            // �����⵽��ײ�壬�Ҳ��Ǵ��������򷵻�����ײ
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                Debug.Log($"��⵽��ײ: {hit.collider.gameObject.name}");
                return true;
            }
        }
        return false;
    }

    // ���ڵ��ԵĿ��ӻ�
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // ������ײ��ⷶΧ
        Vector3 checkPosition = transform.position + Vector3.up * collisionHeightOffset;
        Vector3 displayDirection = moveDirection != Vector3.zero ? moveDirection : Vector3.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(checkPosition, collisionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            checkPosition,
            checkPosition + displayDirection * collisionCheckDistance
        );

        // ������ײ����յ�
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(checkPosition + displayDirection * collisionCheckDistance, collisionRadius);

        // ���Ʋ���������Ϣ
        if (isMoving)
        {
            // ������ʼλ�ú�Ŀ��λ��
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(stepStartPosition, 0.05f);
            Gizmos.DrawLine(stepStartPosition, stepTargetPosition);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(stepTargetPosition, 0.05f);

            // ���Ƶ�ǰ��ֱƫ��
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * currentVerticalOffset);
        }
    }

    // �ⲿ���ý���/�����ƶ�
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
    }
}