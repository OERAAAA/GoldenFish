using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using Unity.XR.CoreUtils;

public class PlayerMovementSystem : MonoBehaviour
{
    [Header("VR����")]
    public XROrigin xrOrigin;
    public Collider leftHandCollider;
    public Collider rightHandCollider;
    public Transform head;

    [Header("�ƶ�����")]
    public float moveDistance = 1f;
    public float moveDuration = 0.5f;
    public int maxMoveCount = 0;
    public bool resetOnNewSession = true;
    [Range(0f, 45f)] public float maxAngleDeviation = 45f; // ���������ƫ��Ƕ�

    [Header("��������")]
    public float triggerRadius = 0.3f;
    public float triggerHeight = 0.5f;

    // ����ʱ״̬
    private bool isMoving;
    private int currentMoveCount;
    private bool handsWereInTrigger;
    private bool requireExit;

    void Start()
    {
        if (resetOnNewSession) currentMoveCount = 0;
    }

    void Update()
    {
        if (maxMoveCount > 0 && currentMoveCount >= maxMoveCount) return;

        Vector3 triggerPos = head.position + Vector3.up * triggerHeight;
        Collider[] hits = Physics.OverlapSphere(triggerPos, triggerRadius);

        bool leftHandIn = System.Array.Exists(hits, col => col == leftHandCollider);
        bool rightHandIn = System.Array.Exists(hits, col => col == rightHandCollider);
        bool handsInTrigger = leftHandIn && rightHandIn;

        if (!isMoving)
        {
            if (requireExit)
            {
                if (!handsInTrigger) requireExit = false;
            }
            else if (handsInTrigger)
            {
                if (!handsWereInTrigger)
                {
                    // �������ͷ��������ƶ�����
                    Vector3 moveDirection = CalculateMoveDirection();
                    StartCoroutine(MovePlayer(moveDirection));
                    requireExit = true;
                }
            }
        }

        handsWereInTrigger = handsInTrigger;
    }

    Vector3 CalculateMoveDirection()
    {
        // ��ȡͷ��ǰ����������������ת��X��Z�ᣨֻ����Y����ת��
        Vector3 headForward = head.forward;
        headForward.y = 0f; // ����ˮƽ����
        headForward.Normalize();

        // ���������ƫ�Ʒ��򣨻���ͷ��ˮƽ����
        Vector3 deviationDirection = headForward;

        // ����ƫ�ƽǶȲ�����maxAngleDeviation
        float angle = Vector3.Angle(Vector3.up, deviationDirection);
        if (angle > maxAngleDeviation)
        {
            // ����Ƕȳ������ƣ�ʹ�������ֵ���������Ƕ���
            deviationDirection = Vector3.Slerp(Vector3.up, deviationDirection, maxAngleDeviation / angle);
        }

        return deviationDirection.normalized;
    }

    IEnumerator MovePlayer(Vector3 direction)
    {
        isMoving = true;
        currentMoveCount++;

        Vector3 startPos = xrOrigin.transform.position;
        Vector3 targetPos = startPos + direction * moveDistance;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            xrOrigin.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        xrOrigin.transform.position = targetPos;
        isMoving = false;

        Debug.Log($"�ƶ���� ({currentMoveCount}/{maxMoveCount}) ����: {direction}");
    }

    public void ResetMoveCount() => currentMoveCount = 0;

    void OnDrawGizmosSelected()
    {
        if (head == null) return;

        Gizmos.color = requireExit ? Color.yellow :
                      (maxMoveCount > 0 && currentMoveCount >= maxMoveCount) ? Color.red : Color.cyan;

        Vector3 triggerPos = head.position + Vector3.up * triggerHeight;
        Gizmos.DrawWireSphere(triggerPos, triggerRadius);

        // �����ƶ�����
        Vector3 moveDir = CalculateMoveDirection();
        Gizmos.DrawLine(triggerPos, triggerPos + moveDir * moveDistance * 0.5f);
    }
}