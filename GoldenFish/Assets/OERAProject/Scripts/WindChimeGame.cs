using UnityEngine;
using UnityEngine.XR;

public class WindChimeGame : MonoBehaviour
{
    [Header("����������")]
    public GameObject windParticlePrefab;
    public Transform windSpawnPlane; // ���ɷ��ƽ��
    public Vector2 spawnSize = new Vector2(5f, 3f); // ���������С
    public Vector2 windSpeedRange = new Vector2(1f, 3f); // ���ٷ�Χ
    public Vector2 spawnIntervalRange = new Vector2(0.5f, 2f); // ���ɼ����Χ

    [Header("�������")]
    public Transform leftHand;
    public Transform rightHand;
    public float catchRadius = 0.3f; // ��׽�뾶

    [Header("Ч������")]
    public AudioClip catchSound;
    public ParticleSystem catchEffect;

    private float nextSpawnTime;

    void Start()
    {
        nextSpawnTime = Time.time + Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    void Update()
    {
        // �����µķ�����
        if (Time.time >= nextSpawnTime)
        {
            SpawnWindParticle();
            nextSpawnTime = Time.time + Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        // ��Ⲷ׽
        DetectCatches();
    }

    void SpawnWindParticle()
    {
        // ������ƽ�������λ��
        Vector3 spawnPos = windSpawnPlane.position +
                          windSpawnPlane.right * Random.Range(-spawnSize.x / 2, spawnSize.x / 2) +
                          windSpawnPlane.up * Random.Range(-spawnSize.y / 2, spawnSize.y / 2);

        GameObject wind = Instantiate(windParticlePrefab, spawnPos, Quaternion.identity);
        WindParticle wp = wind.AddComponent<WindParticle>();

        // ���÷��ٺͷ���(�������)
        wp.speed = Random.Range(windSpeedRange.x, windSpeedRange.y);
        wp.direction = -windSpawnPlane.forward;

        // �Զ�����
        Destroy(wind, 10f);
    }

    void DetectCatches()
    {
        // ��ȡ���������з�����
        WindParticle[] winds = FindObjectsOfType<WindParticle>();

        foreach (WindParticle wind in winds)
        {
            // �������
            if (Vector3.Distance(leftHand.position, wind.transform.position) < catchRadius)
            {
                CatchWind(wind.gameObject, leftHand.position);
                break;
            }

            // �������
            if (Vector3.Distance(rightHand.position, wind.transform.position) < catchRadius)
            {
                CatchWind(wind.gameObject, rightHand.position);
                break;
            }
        }
    }

    void CatchWind(GameObject wind, Vector3 catchPosition)
    {
        // ������Ч
        if (catchSound != null)
            AudioSource.PlayClipAtPoint(catchSound, catchPosition);

        // ��������Ч��
        if (catchEffect != null)
        {
            ParticleSystem effect = Instantiate(catchEffect, catchPosition, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // ���ٷ�����
        Destroy(wind);
    }
}

// ��������Ϊ�ű�
public class WindParticle : MonoBehaviour
{
    public float speed;
    public Vector3 direction;

    void Update()
    {
        // ��ָ�������ƶ�
        transform.position += direction.normalized * speed * Time.deltaTime;
    }
}