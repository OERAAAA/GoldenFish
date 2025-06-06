using UnityEngine;

public class PlayAudioOnce : MonoBehaviour
{
    public string playerTag = "Player";     // 玩家标签
    public AudioClip soundClip;             // 要播放的音频片段
    private bool hasPlayed = false;         // 是否已播放过
    private AudioSource audioSource;        // 音频播放器

    void Start()
    {
        // 添加 AudioSource 组件（如果没有的话）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.clip = soundClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed && other.CompareTag(playerTag))
        {
            audioSource.Play();
            hasPlayed = true;

        }
    }
}
