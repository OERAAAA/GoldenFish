using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using Unity.XR.CoreUtils;

public class FixedZonePuppetMovement : MonoBehaviour
{
    [Header("VR����")]
    public XROrigin xrOrigin;
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("�ƶ�����")]
    public float stepDistance = 0.5f;
    public float stepDuration = 0.8f;
    public float cooldown = 0.5f;

    [Header("���ִ�����������")]
    public Vector3 leftForwardOffset = new Vector3(0.3f, 0.2f, 0.5f);  // ��ǰ��λ��
    public Vector3 leftBackwardOffset = new Vector3(0.3f, 0.2f, -0.5f); // ���λ��
    public float leftZoneRadius = 0.3f;
    public Color leftReadyColor = Color.cyan;
    public Color leftWaitingColor = Color.gray;

    [Header("���ִ�����������")]
    public Vector3 rightForwardOffset = new Vector3(-0.3f, -0.2f, 0.5f);  // ��ǰ��λ��
    public Vector3 rightBackwardOffset = new Vector3(-0.3f, -0.2f, -0.5f); // �Һ�λ��
    public float rightZoneRadius = 0.3f;
    public Color rightReadyColor = Color.magenta;
    public Color rightWaitingColor = Color.gray;

    [Header("���ж�������")]
    public float upwardDistance = 0.1f; // �����ƶ��ľ���
    public float upwardDurationRatio = 0.3f; // �����ƶ���ռ��ʱ�����
    public float downwardDurationRatio = 0.7f; // �����ƶ���ռ��ʱ�����


    [Header("����")]
    public bool showZones = true;

    private GameObject leftZone;
    private GameObject rightZone;
    private bool isMoving;
    private float lastStepTime;
    private bool isForwardPosition = true; // ��ǰ�Ƿ���ǰ��λ��

    void Start()
    {
        CreateZones();
        UpdateZonePositions();
    }

    void CreateZones()
    {
        leftZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(leftZone.GetComponent<Collider>());
        leftZone.transform.localScale = Vector3.one * leftZoneRadius * 2;
        leftZone.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        leftZone.GetComponent<Renderer>().material.color = leftWaitingColor;

        rightZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(rightZone.GetComponent<Collider>());
        rightZone.transform.localScale = Vector3.one * rightZoneRadius * 2;
        rightZone.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        rightZone.GetComponent<Renderer>().material.color = rightWaitingColor;
    }

    void Update()
    {
        if (isMoving || Time.time < lastStepTime + cooldown) return;

        UpdateZonePositions();

        bool leftInZone = Vector3.Distance(leftHand.position, leftZone.transform.position) < leftZoneRadius;
        bool rightInZone = Vector3.Distance(rightHand.position, rightZone.transform.position) < rightZoneRadius;

        leftZone.GetComponent<Renderer>().material.color = leftInZone ? leftReadyColor : leftWaitingColor;
        rightZone.GetComponent<Renderer>().material.color = rightInZone ? rightReadyColor : rightWaitingColor;

        if (leftInZone && rightInZone)
        {
            StartCoroutine(PerformStep());
        }
    }

    void UpdateZonePositions()
    {
        // ���ݵ�ǰ��ǰ�����Ǻ�λ������������
        Vector3 currentLeftOffset = isForwardPosition ? leftForwardOffset : leftBackwardOffset;
        Vector3 currentRightOffset = isForwardPosition ? rightForwardOffset : rightBackwardOffset;

        leftZone.transform.position = head.position + currentLeftOffset;
        rightZone.transform.position = head.position + currentRightOffset;
    }

    IEnumerator PerformStep()
    {
        isMoving = true;
        lastStepTime = Time.time;

        Vector3 startPos = xrOrigin.transform.position;
        Vector3 moveDir = head.forward;
        moveDir.y = 0;
        Vector3 horizontalTarget = startPos + moveDir.normalized * stepDistance;

        // ���������ƶ��ķ���
        float totalHeightChange = upwardDistance;
        float upwardDuration = stepDuration * upwardDurationRatio;
        float downwardDuration = stepDuration * downwardDurationRatio;

        // ��һ�׶Σ������ƶ�
        float elapsed = 0f;
        Vector3 upwardTarget = startPos + moveDir.normalized * (stepDistance * upwardDurationRatio)
                              + Vector3.up * totalHeightChange;

        while (elapsed < upwardDuration)
        {
            xrOrigin.transform.position = Vector3.Lerp(
                startPos,
                upwardTarget,
                Mathf.SmoothStep(0, 1, elapsed / upwardDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // �ڶ��׶Σ������ƶ��ص�ˮƽλ��
        elapsed = 0f;
        Vector3 finalTarget = new Vector3(horizontalTarget.x, startPos.y, horizontalTarget.z);

        while (elapsed < downwardDuration)
        {
            xrOrigin.transform.position = Vector3.Lerp(
                upwardTarget,
                finalTarget,
                Mathf.SmoothStep(0, 1, elapsed / downwardDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        xrOrigin.transform.position = finalTarget;

        // �л�ǰ��λ��
        isForwardPosition = !isForwardPosition;

        // ����������ɫ
        leftZone.GetComponent<Renderer>().material.color = leftWaitingColor;
        rightZone.GetComponent<Renderer>().material.color = rightWaitingColor;

        isMoving = false;
    }

    void OnDestroy()
    {
        if (leftZone) Destroy(leftZone);
        if (rightZone) Destroy(rightZone);
    }

    void OnDrawGizmos()
    {
        if (!showZones || head == null) return;

        // ����ǰ������
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(head.position + leftForwardOffset, leftZoneRadius);
        Gizmos.DrawWireSphere(head.position + rightForwardOffset, rightZoneRadius);

        // ���ƺ�����
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(head.position + leftBackwardOffset, leftZoneRadius);
        Gizmos.DrawWireSphere(head.position + rightBackwardOffset, rightZoneRadius);

        // ���Ƶ�ǰ�����
        Gizmos.color = Color.green;
        Vector3 currentLeft = isForwardPosition ? leftForwardOffset : leftBackwardOffset;
        Vector3 currentRight = isForwardPosition ? rightForwardOffset : rightBackwardOffset;
        Gizmos.DrawWireSphere(head.position + currentLeft, leftZoneRadius * 1.1f);
        Gizmos.DrawWireSphere(head.position + currentRight, rightZoneRadius * 1.1f);
    }
}