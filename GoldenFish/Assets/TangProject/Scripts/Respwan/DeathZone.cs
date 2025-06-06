using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ReManager.Instance.RespawnPlayer(other.gameObject);

        }
    }
}
