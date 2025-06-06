using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Header("Respawn Settings")]
    public string playerTag = "Player";  // 玩家标签

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // 通知 ReManager 这个区域是新的重生点
            ReManager.Instance.UpdateRespawnPoint(transform.position);

        }
    }
}
