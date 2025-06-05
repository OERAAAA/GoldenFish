using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    private static LevelLoader instance;

    void Awake()
    {
        // ����ģʽȷ��ֻ��һ��ʵ������
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // �糡��������
        }
        else
        {
            Destroy(gameObject); // �����ظ�ʵ��
        }
    }

    void Update()
    {
        // ���P������
        if (Input.GetKeyDown(KeyCode.P))
        {
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        int totalScenes = SceneManager.sceneCountInBuildSettings;

        // ѭ�����������������ǰ����󳡾���ص�0��
        if (nextSceneIndex >= totalScenes)
        {
            nextSceneIndex = 0;
        }

        SceneManager.LoadScene(nextSceneIndex);
    }
}