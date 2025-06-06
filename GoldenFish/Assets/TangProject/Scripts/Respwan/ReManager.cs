using UnityEngine;

public class ReManager : MonoBehaviour
{
    public static ReManager Instance { get; private set; }

    private Vector3 currentRespawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentRespawnPoint = Vector3.zero; // 默认起始点
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 设置当前存档点位置
    public void UpdateRespawnPoint(Vector3 newRespawnPoint)
    {
        currentRespawnPoint = newRespawnPoint;
    }

    // 让玩家在当前存档点重生
    public void RespawnPlayer(GameObject player)
    {
        player.transform.position = currentRespawnPoint;

        // 可选：重置速度
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


    }
}
