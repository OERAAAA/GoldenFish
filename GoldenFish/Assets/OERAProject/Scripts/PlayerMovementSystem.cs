using UnityEngine;
using UnityEngine.XR;
using Unity.XR.PXR;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using FMODUnity;

public class PlayerMovementSystem : MonoBehaviour
{
    [Header("VR配置")]
    public XROrigin xrOrigin;
    public Collider leftHandCollider;
    public Collider rightHandCollider;
    public Transform head;
    public Camera vrCamera;
    
[Header("移动启动延迟")]
public float startDelay = 3f;
private float startTime;



    [Header("移动参数")]
    public float moveDistance = 1f;
    public float moveDuration = 0.5f;
    public int maxMoveCount = 0;
    public bool resetOnNewSession = true;
    [Range(0f, 45f)] public float maxAngleDeviation = 45f;

    [Header("URP Renderer Feature控制")]
    public UniversalRendererData rendererData;
    public string[] featuresToDisable;
    public int disableAfterMoves = 3;
    private bool hasDisabledFeatures = false;

    [Header("触发区域")]
    public float triggerRadius = 0.3f;
    public float triggerHeight = 0.5f;
    public float frontSphereDistance = 0.5f;
    public float sphereRadius = 0.4f;

    [Header("阶段触发物体")]
    public GameObject objectToEnableOn2ndMove;
    public GameObject objectToEnableOn5thMove;

    [Header("植物生长")]
    public Transform plantModel;
    public float growthPerMove = 0.1f;

    private bool isMoving;
    private int currentMoveCount;
    private bool handsWereInTrigger;
    private bool requireExit;

    [EventRef] public string catchSoundEvent;
    [EventRef] public string blossom;

    [Header("动画控制")]
    public Animator targetAnimator;
    public string bloomParameterName = "bloom";

    [Header("渐变出现的物体")]
    public GameObject scaleUpObject;
    public Vector3 targetScale = Vector3.one;
    public float scaleUpDuration = 0.5f;
    public float scaleUpDelay = 1f;

    private float lastMoveTime = -Mathf.Infinity;

void Start()
{
    if (resetOnNewSession) currentMoveCount = 0;
    startTime = Time.time;
}


void Update()
{
    // 延迟未到，不进行任何移动判定
    if (Time.time - startTime < startDelay)
        return;

    if (maxMoveCount > 0 && currentMoveCount >= maxMoveCount) return;

    // 其余逻辑保持不变
    Vector3 headPos = head.position;
    Vector3 headForward = GetHorizontalForward(head);

    bool leftHandIn = IsHandInDoubleSphere(leftHandCollider.transform.position, headPos, headForward);
    bool rightHandIn = IsHandInDoubleSphere(rightHandCollider.transform.position, headPos, headForward);
    bool handsInTrigger = leftHandIn && rightHandIn;

    if (!isMoving && Time.time - lastMoveTime >= moveDuration)
    {
        if (requireExit)
        {
            if (!handsInTrigger) requireExit = false;
        }
        else if (handsInTrigger)
        {
            if (!handsWereInTrigger)
            {
                RuntimeManager.PlayOneShot(catchSoundEvent);
                Vector3 moveDirection = CalculateMoveDirection();
                StartCoroutine(MovePlayer(moveDirection));
                requireExit = true;
                lastMoveTime = Time.time;
            }
        }
    }

    handsWereInTrigger = handsInTrigger;
}


    // 获取水平方向的前向向量（忽略头部上下倾斜）
    private Vector3 GetHorizontalForward(Transform head)
    {
        Vector3 forward = head.forward;
        forward.y = 0; // 忽略垂直分量
        return forward.normalized;
    }

    private bool IsHandInDoubleSphere(Vector3 handPos, Vector3 headPos, Vector3 headForward)
    {
        Vector3 topSphere = headPos + Vector3.up * triggerHeight;
        Vector3 frontSphere = topSphere + headForward * frontSphereDistance;

        return Vector3.Distance(handPos, topSphere) < sphereRadius ||
               Vector3.Distance(handPos, frontSphere) < sphereRadius;
    }

    Vector3 CalculateMoveDirection()
    {
        Vector3 headUp = head.up;
        float angle = Vector3.Angle(Vector3.up, headUp);

        if (angle > maxAngleDeviation)
            headUp = Vector3.Slerp(Vector3.up, headUp, maxAngleDeviation / angle);

        return headUp.normalized;
    }

    IEnumerator MovePlayer(Vector3 direction)
    {
        isMoving = true;
        currentMoveCount++;

        if (currentMoveCount == 1 && objectToEnableOn2ndMove != null)
        {
            objectToEnableOn2ndMove.SetActive(true);

        }
        else if (currentMoveCount == 8 && objectToEnableOn5thMove != null)
        {
            objectToEnableOn5thMove.SetActive(true);

        }

        if (currentMoveCount >= disableAfterMoves && !hasDisabledFeatures)
            DisableRendererFeatures();

        Vector3 startPos = xrOrigin.transform.position;
        Vector3 targetPos = startPos + direction * moveDistance;

        Vector3 plantStartScale = plantModel != null ? plantModel.localScale : Vector3.zero;
        Vector3 plantTargetScale = plantModel != null ? new Vector3(
            plantStartScale.x,
            plantStartScale.y + growthPerMove,
            plantStartScale.z
        ) : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            xrOrigin.transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (plantModel != null)
                plantModel.localScale = Vector3.Lerp(plantStartScale, plantTargetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        xrOrigin.transform.position = targetPos;

        if (plantModel != null)
            plantModel.localScale = plantTargetScale;

        isMoving = false;

    }

    void DisableRendererFeatures()
    {
        if (rendererData == null)
        {

            return;
        }

        bool anyFeatureDisabled = false;
        foreach (var featureName in featuresToDisable)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature != null && feature.name == featureName)
                {
                    feature.SetActive(false);

                    anyFeatureDisabled = true;
                    break;
                }
            }
        }

        if (anyFeatureDisabled)
        {
            rendererData.SetDirty();
            GraphicsSettings.renderPipelineAsset = GraphicsSettings.renderPipelineAsset;

            if (targetAnimator != null)
            {
                targetAnimator.SetBool(bloomParameterName, true);

            }


            if (scaleUpObject != null)
            {
                scaleUpObject.SetActive(true);
                StartCoroutine(ScaleUpObject(scaleUpObject, targetScale, scaleUpDuration, scaleUpDelay));
            }

            RuntimeManager.PlayOneShot(blossom);
            hasDisabledFeatures = true;

        }
    }

    public void ResetMoveCount()
    {
        currentMoveCount = 0;
        hasDisabledFeatures = false;

        if (rendererData != null && featuresToDisable != null)
        {
            foreach (var featureName in featuresToDisable)
            {
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature != null && feature.name == featureName)
                    {
                        feature.SetActive(true);
                        break;
                    }
                }
            }
            rendererData.SetDirty();
            GraphicsSettings.renderPipelineAsset = GraphicsSettings.renderPipelineAsset;

        }
    }

    void OnDrawGizmosSelected()
    {
        if (head == null) return;

        Gizmos.color = requireExit ? Color.yellow :
                      (maxMoveCount > 0 && currentMoveCount >= maxMoveCount) ? Color.red : Color.cyan;

        Vector3 headPos = head.position;
        Vector3 headForward = GetHorizontalForward(head);
        Vector3 topSphere = headPos + Vector3.up * triggerHeight;
        Vector3 frontSphere = topSphere + headForward * frontSphereDistance;

        Gizmos.DrawWireSphere(topSphere, sphereRadius);
        Gizmos.DrawWireSphere(frontSphere, sphereRadius);
        Gizmos.DrawLine(topSphere, frontSphere);

        Gizmos.DrawLine(topSphere, topSphere + Vector3.up * moveDistance * 0.5f);
    }

    IEnumerator ScaleUpObject(GameObject obj, Vector3 finalScale, float duration, float delay)
    {
        obj.SetActive(true);
        Transform objTransform = obj.transform;
        Vector3 initialScale = Vector3.zero;
        float elapsed = 0f;

        objTransform.localScale = initialScale;
        yield return new WaitForSeconds(delay);
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            objTransform.localScale = Vector3.Lerp(initialScale, finalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        objTransform.localScale = finalScale;
    }
}