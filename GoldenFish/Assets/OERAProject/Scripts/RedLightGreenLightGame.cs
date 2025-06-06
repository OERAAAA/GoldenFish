using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using Unity.XR.CoreUtils;
using FMODUnity;

public class RedLightGreenLightGame : MonoBehaviour
{
    [Header("���������")]
    public GameObject toyBear;
    public float activationDistance = 5f;
    public float safeZoneDistance = 2f;
    public float turnDuration = 2f;

    [Header("ʱ������")]
    public float warningDuration = 1f;
    public float[] possibleRedDurations = { 2.0f, 2.3f, 2.7f, 3.0f, 3.3f, 3.7f, 4.0f };
    public float[] possibleGreenDurations = { 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f };

    [Header("��Ҽ������")]
    public float headMovementThreshold = 0.05f;
    public float handMovementThreshold = 0.1f;
    public float checkInterval = 0.2f;

    [Header("�Ӿ�Ч��")]
    public Light directionalLight;
    public float dimIntensity = 0.3f;
    private float originalIntensity;
    public Color warningColor = Color.yellow;
    public Color redLightColor = Color.red;
    public Color greenLightColor = Color.green;

    [Header("�������")]
    public Transform xrOrigin;
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public Vector3 startPosition;

    [Header("��ȫ��������")]
    public float safeZoneBuffer = 0.5f;

    private bool isInSafeZone = false;
    private enum GameState { Inactive, GreenLight, RedLight, Warning }
    private GameState currentState = GameState.Inactive;
    private Vector3 lastHeadLocalPosition;
    private Vector3 lastLeftHandLocalPosition;
    private Vector3 lastRightHandLocalPosition;
    private float stateTimer;
    private bool isTurning = false;
    private bool isFacingPlayer = false;
    private float currentGreenLightDuration;
    private float currentRedLightDuration;

    [Header("FMOD����")]
    [EventRef] public string alarm;

    [Header("首次警报触发声音")]
    public GameObject firstAlarmItem;
    private bool hasTriggeredFirstAlarm = false;
    


    void Start()
    {
        originalIntensity = directionalLight.intensity;
        startPosition = xrOrigin.position;
        SetBearFacingPlayer(false);
        directionalLight.color = Color.white;
        directionalLight.intensity = originalIntensity;
        RecordCurrentPositions();
        GenerateRandomDurations();
    }

    void GenerateRandomDurations()
    {
        currentRedLightDuration = possibleRedDurations[Random.Range(0, possibleRedDurations.Length)];
        currentGreenLightDuration = possibleGreenDurations[Random.Range(0, possibleGreenDurations.Length)];
    }

    void Update()
    {
        float distanceToBear = Vector3.Distance(xrOrigin.position, toyBear.transform.position);

        if (currentState != GameState.Inactive)
        {
            if (distanceToBear <= safeZoneDistance && !isInSafeZone)
            {
                isInSafeZone = true;
                ExitGame();
                return;
            }
            else if (distanceToBear > (safeZoneDistance + safeZoneBuffer))
            {
                isInSafeZone = false;
            }
        }

        if (currentState == GameState.Inactive &&
            distanceToBear <= activationDistance &&
            distanceToBear > (safeZoneDistance + safeZoneBuffer))
        {
            StartGame();
        }

        if (currentState != GameState.Inactive && !isTurning)
        {
            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case GameState.GreenLight:
                    if (stateTimer <= 0) StartCoroutine(TransitionToRedLight());
                    break;
                case GameState.RedLight:
                    if (stateTimer <= 0) StartCoroutine(TransitionToGreenLight());
                    break;
                case GameState.Warning:
                    if (stateTimer <= 0) StartCoroutine(TurnToFacePlayer());
                    break;
            }
        }
    }

    void ExitGame()
    {
        if (currentState == GameState.Inactive) return;

        currentState = GameState.Inactive;
        directionalLight.intensity = originalIntensity;
        directionalLight.color = Color.white;
        StopAllCoroutines();
    }

    void StartGame()
    {
        GenerateRandomDurations();
        currentState = GameState.GreenLight;
        stateTimer = currentGreenLightDuration;
        directionalLight.color = greenLightColor;
        directionalLight.intensity = originalIntensity;
        StartCoroutine(MonitorPlayerMovement());

    }

IEnumerator TransitionToRedLight()
{
    currentState = GameState.Warning;
    stateTimer = warningDuration;
    directionalLight.intensity = dimIntensity;
    directionalLight.color = warningColor;
    RuntimeManager.PlayOneShot(alarm);


    // 首次触发警报，激活物品
    if (!hasTriggeredFirstAlarm)
    {
        hasTriggeredFirstAlarm = true;
        if (firstAlarmItem != null)
        {
            firstAlarmItem.SetActive(true);

        }
    }

    yield return new WaitForSeconds(warningDuration);
    StartCoroutine(TurnToFacePlayer());
}


    IEnumerator TurnToFacePlayer()
    {
        if (isTurning || isFacingPlayer) yield break;
        isTurning = true;

        float elapsed = 0f;
        Quaternion startRotation = toyBear.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 180, 0);

        while (elapsed < turnDuration)
        {
            toyBear.transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                elapsed / turnDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        toyBear.transform.rotation = endRotation;
        isFacingPlayer = true;
        GenerateRandomDurations(); // ��������ƿ�ʼʱ��������ʱ��
        currentState = GameState.RedLight;
        directionalLight.color = redLightColor;
        directionalLight.intensity = originalIntensity;
        stateTimer = currentRedLightDuration;
        isTurning = false;

        RecordCurrentPositions();
    }

    IEnumerator TransitionToGreenLight()
    {
        if (isTurning || !isFacingPlayer) yield break;
        isTurning = true;

        float elapsed = 0f;
        Quaternion startRotation = toyBear.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 180, 0);

        while (elapsed < turnDuration)
        {
            toyBear.transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                elapsed / turnDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        toyBear.transform.rotation = endRotation;
        isFacingPlayer = false;
        GenerateRandomDurations(); // �������̵ƿ�ʼʱ��������ʱ��
        currentState = GameState.GreenLight;
        directionalLight.color = greenLightColor;
        stateTimer = currentGreenLightDuration;
        isTurning = false;

    }

    IEnumerator MonitorPlayerMovement()
    {
        while (currentState != GameState.Inactive)
        {
            if (currentState == GameState.RedLight && isFacingPlayer)
            {
                Vector3 headLocalPos = head.localPosition;
                Vector3 leftHandLocalPos = leftHand.localPosition;
                Vector3 rightHandLocalPos = rightHand.localPosition;

                float headMovement = Vector3.Distance(headLocalPos, lastHeadLocalPosition);
                float leftHandMovement = Vector3.Distance(leftHandLocalPos, lastLeftHandLocalPosition);
                float rightHandMovement = Vector3.Distance(rightHandLocalPos, lastRightHandLocalPosition);

                if (headMovement > headMovementThreshold ||
                    leftHandMovement > handMovementThreshold ||
                    rightHandMovement > handMovementThreshold)
                {
                    PlayerCaughtMoving();
                }
            }
            RecordCurrentPositions();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    void RecordCurrentPositions()
    {
        lastHeadLocalPosition = head.localPosition;
        lastLeftHandLocalPosition = leftHand.localPosition;
        lastRightHandLocalPosition = rightHand.localPosition;
    }

    void PlayerCaughtMoving()
    {

        xrOrigin.position = startPosition;
        SetBearFacingPlayer(false);
        currentState = GameState.Inactive;
        directionalLight.intensity = originalIntensity;
        directionalLight.color = Color.white;
    }

    void SetBearFacingPlayer(bool facing)
    {
        if (facing)
        {
            toyBear.transform.rotation = Quaternion.LookRotation(
                new Vector3(
                    xrOrigin.position.x,
                    toyBear.transform.position.y,
                    xrOrigin.position.z
                ) - toyBear.transform.position
            );
        }
        else
        {
            toyBear.transform.rotation = Quaternion.LookRotation(
                toyBear.transform.position - new Vector3(
                    xrOrigin.position.x,
                    toyBear.transform.position.y,
                    xrOrigin.position.z
                )
            );
        }
        isFacingPlayer = facing;
    }

    void OnDrawGizmos()
    {
        if (toyBear != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(toyBear.transform.position, activationDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(toyBear.transform.position, safeZoneDistance);
        }
    }
}