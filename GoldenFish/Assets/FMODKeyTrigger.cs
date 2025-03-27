using UnityEngine;
using FMODUnity;

public class FMODKeyTrigger : MonoBehaviour
{
    [SerializeField] private string eventPath = "event:/SoundFX/FX_GlassBall";

    private void PlayBallSound()
    {
        try
        {
            // ����1���󶨵���ǰ���壨�����ƶ���
            RuntimeManager.PlayOneShotAttached(eventPath, gameObject);

            // ����2���ڵ�ǰλ�ò��ţ��������ƶ���
            // RuntimeManager.PlayOneShot(eventPath, transform.position);

            Debug.Log($"��������: {eventPath} λ��: {transform.position}");
        }
        catch (EventNotFoundException e)
        {
            Debug.LogError($"FMOD�¼�δ�ҵ�: {eventPath}\n{e.Message}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            PlayBallSound();
        }
    }
}