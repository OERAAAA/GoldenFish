using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinkInWater : MonoBehaviour
{
    public GameObject prefab;
    public GameObject referenceObject;
    public GameObject character;
    public float sinkSpeed = 2f;
    public int numberOfObjects = 10;
    public float spawnInterval = 1f;
    public float shortInterval = 0.3f;
    public int sequentialDropCount = 3; // 前几颗采用逐个吃掉后生成
    public float waitAfterEat = 1.5f;    // 吃掉一个后等待再生成下一个
    public float initialCheckTimeout = 5f; // 初始测试等待时间
    public float delayBeforeSequential = 1f;

    private float groundHeight;
    private Queue<GameObject> activeObjects = new Queue<GameObject>();
    private int currentSpawnIndex = 0;
    private Collider referenceCollider;

    private void Start()
    {
        referenceCollider = referenceObject.GetComponent<Collider>();
        if (referenceCollider == null)
        {

            return;
        }

        groundHeight = character.transform.position.y;
        StartCoroutine(InitialCheckSpawn());
    }

private IEnumerator InitialCheckSpawn()
{
    GameObject testObj = null;
    bool eaten = false;

    while (!eaten)
    {
        Vector3 spawnPos = GetRandomSpawnPosition(referenceCollider);
        testObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeObjects.Enqueue(testObj);
        StartCoroutine(SinkObject(testObj));

        float timer = 0f;

        while (timer < initialCheckTimeout)
        {
            if (!activeObjects.Contains(testObj))
            {
                eaten = true;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (!eaten)
        {
            if (activeObjects.Contains(testObj))
            {
                activeObjects.Dequeue();
            }
            Destroy(testObj);
        }
    }

    // ✅ 成功吃掉后，先等待一段时间再进入原始逻辑
    yield return new WaitForSeconds(delayBeforeSequential); // <-- 新增这行

    StartCoroutine(SpawnSequentially());
}


    private IEnumerator SpawnSequentially()
    {
        while (currentSpawnIndex < sequentialDropCount)
        {
            SpawnObject(currentSpawnIndex);
            currentSpawnIndex++;

            yield return new WaitUntil(() => activeObjects.Count == 0);
            yield return new WaitForSeconds(waitAfterEat);
        }

        StartCoroutine(SpawnRemaining());
    }

    private IEnumerator SpawnRemaining()
    {
        for (int i = currentSpawnIndex; i < numberOfObjects; i++)
        {
            SpawnObject(i);

            if (i == 3 || i == 6)
                yield return new WaitForSeconds(shortInterval);
            else
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnObject(int index)
    {
        Vector3 spawnPos = GetRandomSpawnPosition(referenceCollider);
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeObjects.Enqueue(obj);
        StartCoroutine(SinkObject(obj));
    }

    private Vector3 GetRandomSpawnPosition(Collider col)
    {
        Vector3 min = col.bounds.min;
        Vector3 max = col.bounds.max;
        float x = Random.Range(min.x, max.x);
        float z = Random.Range(min.z, max.z);
        float y = max.y;
        return new Vector3(x, y, z);
    }

    private IEnumerator SinkObject(GameObject obj)
    {
        while (obj.transform.position.y > groundHeight)
        {
            obj.transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
            yield return null;
        }

        obj.transform.position = new Vector3(obj.transform.position.x, groundHeight, obj.transform.position.z);
        // 不销毁，由 Eat 控制销毁
    }

    // 外部调用：当一个物体被吃掉（销毁）时触发
    public void NotifyObjectEaten(GameObject obj)
    {
        if (activeObjects.Contains(obj))
        {
            activeObjects.Dequeue();
        }
    }
}
