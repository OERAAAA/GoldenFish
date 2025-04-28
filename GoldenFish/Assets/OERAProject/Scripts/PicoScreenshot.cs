using UnityEngine;
using System.Collections;
using UnityEngine.XR;

public class PicoScreenshot : MonoBehaviour
{
    [Header("��ͼ����")]
    public GameObject screenshotPrefab;  // ��ƬԤ���壨����ʹ��Quad��
    public float photoWidth = 0.3f;      // ������Ƭ��ȣ��ף�

    [Header("��Ƭǽ����")]
    public Transform photoWall;          // ��Ƭǽ�ĸ�����
    public Vector2Int gridSize = new Vector2Int(4, 3); // ��������
    public Vector2 spacing = new Vector2(0.05f, 0.05f); // ��Ƭ���

    private Texture2D[] savedTextures;   // �洢���н�ͼ
    private int currentPhotoIndex = 0;   // ��ǰ��Ƭ����
    private Vector3[,] photoPositions;   // Ԥ�������Ƭλ������
    private bool isTriggerPressed = false;

    void Start()
    {
        InitializePhotoWall();
    }

    void Update()
    {
        CheckControllerInput();
    }

    void InitializePhotoWall()
    {
        savedTextures = new Texture2D[gridSize.x * gridSize.y];
        photoPositions = new Vector3[gridSize.x, gridSize.y];

        // ������Ƭǽ���½����λ�ã���4:3����������
        Vector3 startPos = photoWall.position
                         - photoWall.right * (gridSize.x - 1) * (photoWidth + spacing.x) / 2
                         - photoWall.up * (gridSize.y - 1) * (photoWidth * 0.75f + spacing.y) / 2;

        // Ԥ����������Ƭλ��
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                photoPositions[x, y] = startPos
                                     + photoWall.right * x * (photoWidth + spacing.x)
                                     + photoWall.up * y * (photoWidth * 0.75f + spacing.y);
            }
        }
    }

    void CheckControllerInput()
    {
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerValue))
        {
            if (triggerValue && !isTriggerPressed)
            {
                isTriggerPressed = true;
                TakeScreenshot();
            }
            else if (!triggerValue && isTriggerPressed)
            {
                isTriggerPressed = false;
            }
        }
    }

    void TakeScreenshot()
    {
        StartCoroutine(CaptureAndCrop());
    }

    IEnumerator CaptureAndCrop()
    {
        yield return new WaitForEndOfFrame();

        // ԭʼ��ͼ
        Texture2D fullTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        fullTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        fullTex.Apply();

        // ����ü����򣨱��ֿ�Ȳ��䣬�ü��߶ȣ�
        int targetHeight = Screen.width * 3 / 4; // ����4:3����߶�
        int cropY = (Screen.height - targetHeight) / 2; // ���¸��õ��Ĳ���

        // ����4:3����
        Texture2D croppedTex = new Texture2D(Screen.width, targetHeight, TextureFormat.RGBA32, false);

        // �ü����²���
        Color[] pixels = fullTex.GetPixels(0, cropY, Screen.width, targetHeight);
        croppedTex.SetPixels(pixels);
        croppedTex.Apply();
        Destroy(fullTex);

        // ���浽��Ƭǽ
        SavePhotoToWall(croppedTex);
    }

    void SavePhotoToWall(Texture2D newPhoto)
    {
        // �洢����Ƭ
        savedTextures[currentPhotoIndex] = newPhoto;
        currentPhotoIndex = (currentPhotoIndex + 1) % (gridSize.x * gridSize.y);

        // ������Ƭǽ��ʾ
        UpdatePhotoWallDisplay();
    }

    void UpdatePhotoWallDisplay()
    {
        // ���������Ƭ
        foreach (Transform child in photoWall)
        {
            Destroy(child.gameObject);
        }

        // ��������������Ƭ
        for (int i = 0; i < savedTextures.Length; i++)
        {
            if (savedTextures[i] == null) continue;

            int x = i % gridSize.x;
            int y = i / gridSize.x;
            DisplayPhotoOnWall(savedTextures[i], photoPositions[x, y]);
        }
    }

    void DisplayPhotoOnWall(Texture2D texture, Vector3 position)
    {
        GameObject photo = Instantiate(screenshotPrefab, position, Quaternion.identity, photoWall);
        photo.transform.rotation = photoWall.rotation;

        // �̶�4:3��ʾ����
        photo.transform.localScale = new Vector3(photoWidth, photoWidth * 0.75f, 0.01f);

        // Ӧ�ñ�׼����
        Material newMat = new Material(Shader.Find("Unlit/Texture"));
        newMat.mainTexture = texture;
        photo.GetComponent<Renderer>().material = newMat;
    }
}