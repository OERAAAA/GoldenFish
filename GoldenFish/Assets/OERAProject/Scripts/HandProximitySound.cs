using UnityEngine;
using FMODUnity;

public class HandProximityEmitterController : MonoBehaviour
{
    [Header("��������")]
    public float activationDistance = 0.3f;
    public float cooldownTime = 0.2f;

    [Header("�ο�����")]
    public Transform headTransform;
    public StudioEventEmitter proximityEmitter;

    private bool isInRange = false;
    private float lastToggleTime;
    private FMOD.ATTRIBUTES_3D attributes;

    void Update()
    {
        if (headTransform == null || proximityEmitter == null)
            return;

        // ���㵱ǰ����
        float currentDistance = Vector3.Distance(transform.position, headTransform.position);
        bool shouldBePlaying = currentDistance <= activationDistance;

        // ֻ��״̬�ı�ʱ����(����ȴʱ��)
        if (shouldBePlaying != isInRange && Time.time > lastToggleTime + cooldownTime)
        {
            isInRange = shouldBePlaying;
            lastToggleTime = Time.time;

            if (isInRange)
            {
                proximityEmitter.Play();
                Debug.Log("��ʼ����3D����");
            }
            else
            {
                proximityEmitter.AllowFadeout = true;
                proximityEmitter.Stop();
                Debug.Log("ֹͣ����3D����");
            }
        }

        // ����3D����
        if (proximityEmitter.IsPlaying())
        {
            attributes = RuntimeUtils.To3DAttributes(transform);
            proximityEmitter.EventInstance.set3DAttributes(attributes);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (headTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(headTransform.position, activationDistance);
        }
    }
}