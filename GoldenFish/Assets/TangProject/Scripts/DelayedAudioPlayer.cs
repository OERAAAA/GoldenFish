using UnityEngine;
using System.Collections;

public class DelayedAudioPlayer : MonoBehaviour
{
    public AudioSource audioSource;  // 拖到 Inspector 中或在代码中自动获取
    public float delaySeconds = 1f;

    void Start()
    {
        StartCoroutine(PlayAudioAfterDelay());
    }

    IEnumerator PlayAudioAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);

        if (audioSource != null)
        {
            audioSource.Play();
        }

    }
}
