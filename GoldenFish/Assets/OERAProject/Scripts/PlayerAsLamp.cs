using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Light))]
public class PlayerAsLamp : MonoBehaviour
{
    [Header("��Դ����")]
    [Range(1, 30)] public float lightRange = 10f;       // ���շ�Χ
    [Range(1, 180)] public float lightAngle = 60f;      // ���սǶ�
    [Range(0, 10)] public float lightIntensity = 3f;    // ����ǿ��
    public Color lightColor = new Color(1f, 0.95f, 0.8f); // ů��ɫ

    [Header("����Ч��")]
    public float flickerAmount = 0.05f;  // �ƹ���˸ǿ��
    public float flickerSpeed = 3f;      // ��˸�ٶ�

    private Light lampLight;
    private Transform headTransform;
    private float baseIntensity;
    private float randomSeed;

    void Start()
    {
        // ��ȡVRͷ������������ı任
        headTransform = Camera.main.transform;

        // ��ʼ����Դ���
        lampLight = GetComponent<Light>();
        lampLight.type = LightType.Spot;
        lampLight.range = lightRange;
        lampLight.spotAngle = lightAngle;
        lampLight.intensity = lightIntensity;
        lampLight.color = lightColor;
        lampLight.shadows = LightShadows.Soft;

        baseIntensity = lightIntensity;
        randomSeed = Random.Range(0f, 100f);

        // ����Դ���ӵ�ͷ�����������λ�ã�
        transform.SetParent(headTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        // ʹ��Դ��ȫ����ͷ����ת
        transform.rotation = headTransform.rotation;

        // ģ��ƹ���˸Ч��
        if (flickerAmount > 0)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, randomSeed);
            lampLight.intensity = baseIntensity * (1f + (noise - 0.5f) * flickerAmount);
        }

        // �����ã������������ղ���
        if (Input.GetKey(KeyCode.UpArrow)) lightRange += Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.DownArrow)) lightRange -= Time.deltaTime * 5f;
    }

    // ���ⲿ���õĲ������÷���
    public void SetLightParameters(float range, float angle, float intensity)
    {
        lightRange = Mathf.Clamp(range, 1, 30);
        lightAngle = Mathf.Clamp(angle, 1, 180);
        lightIntensity = Mathf.Clamp(intensity, 0, 10);

        lampLight.range = lightRange;
        lampLight.spotAngle = lightAngle;
        baseIntensity = lightIntensity;
    }
}