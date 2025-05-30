using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class PhotoCapture : MonoBehaviour
{
    [Header("Settings")]
    public int maxPhotosPerRow = 5;          // ÿ�������Ƭ����
    public float photoWidth = 0.3f;          // ��Ƭ��ȣ���λ���ף�
    public float spacing = 0.05f;            // ��Ƭ����С���
    public float maxRotation = 15f;          // ��������ת�Ƕ�
    public float maxOffset = 0.1f;           // ������ƫ����
    public float photoForwardOffset = 0.01f; // ��Ƭ�ڷ��߷����ƫ�ƣ�����Z-fighting��
    public float topMargin = 0.5f;           // �����߾�
    public int maxPhotos = 20;               // �����Ƭ����
    public LayerMask captureLayerMask;       // ������Ҫ��׽�Ĳ�

    [Header("References")]
    public Transform photoWall;             // ������Ƭ��ƽ��
    public Camera captureCamera;            // ���ڽ�ͼ�����

    private List<GameObject> photos = new List<GameObject>();
    private List<Vector3> gridPositions = new List<Vector3>(); // �洢��������λ��
    private int totalPhotosTaken = 0;       // �ܹ��������Ƭ����
    private bool isTriggerPressed = false;  // ���ٰ��״̬
    private bool canTakePhoto = true;       // �����Ƿ���������
    private float rowHeight = 0f;           // �иߣ�������Ƭ�������㣩
    private int originalCullingMask;        // ��¼ԭʼ�����culling mask

    void Start()
    {
        // �����иߣ�������Ƭ��߱ȣ�
        rowHeight = photoWidth * (Screen.height / (float)Screen.width);

        // Ԥ�ȼ�����������λ��
        PrecalculateGridPositions();

        // ��¼ԭʼ�����culling mask
        if (captureCamera != null)
        {
            originalCullingMask = captureCamera.cullingMask;
        }
    }

    void PrecalculateGridPositions()
    {
        gridPositions.Clear();

        int rows = Mathf.CeilToInt(maxPhotos / (float)maxPhotosPerRow);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < maxPhotosPerRow; col++)
            {
                if (gridPositions.Count >= maxPhotos) break;

                float xPos = col * (photoWidth + spacing);
                float yPos = -row * (rowHeight + spacing) - topMargin;
                gridPositions.Add(new Vector3(xPos, yPos, 0));
            }
        }
    }

    void Update()
    {
        // ������ֱ����
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed))
        {
            // ֻ�ڰ�����ͷ�״̬��Ϊ����״̬ʱ����
            if (triggerPressed && !isTriggerPressed && canTakePhoto)
            {
                StartCoroutine(CapturePhoto());
                canTakePhoto = false; // ��ֹ��������
            }

            // ������ͷ�ʱ������������
            if (!triggerPressed)
            {
                canTakePhoto = true;
            }

            isTriggerPressed = triggerPressed; // ����״̬
        }
    }

    IEnumerator CapturePhoto()
    {
        // �ȴ���Ⱦ����
        yield return new WaitForEndOfFrame();

        // ������ͼ����
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        Camera mainCamera = Camera.main;

        // ������ʱ������ڽ�ͼ
        captureCamera.CopyFrom(mainCamera);

        // Ӧ���Զ����culling mask���ų�UI��
        if (captureLayerMask != 0)
        {
            captureCamera.cullingMask = captureLayerMask;
        }

        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        // ��ȡ��Ⱦ����
        RenderTexture.active = renderTexture;
        Texture2D photoTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        photoTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        photoTexture.Apply();
        RenderTexture.active = null;

        // ������Դ
        captureCamera.targetTexture = null;

        // �ָ�ԭʼculling mask
        captureCamera.cullingMask = originalCullingMask;

        Destroy(renderTexture);

        // ������Ƭ����
        CreatePhotoObject(photoTexture);

        // ������Ƭ����
        totalPhotosTaken++;
    }

    void CreatePhotoObject(Texture2D photoTexture)
    {
        // ������Ƭ����
        Material photoMaterial = new Material(Shader.Find("Unlit/Texture"));
        photoMaterial.mainTexture = photoTexture;

        // ������ƬQuad
        GameObject photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(photo.GetComponent<Collider>()); // �Ƴ���ײ��
        photo.name = "Photo_" + totalPhotosTaken;
        photo.transform.SetParent(photoWall);

        // ���ò��ʺʹ�С
        photo.GetComponent<Renderer>().material = photoMaterial;
        photo.transform.localScale = new Vector3(photoWidth, photoWidth * (photoTexture.height / (float)photoTexture.width), 1);

        // ����λ�ú���ת
        PlacePhotoWithRandomness(photo.transform);

        // ������Ƭ�б�
        ManagePhotoList(photo);
    }

    void PlacePhotoWithRandomness(Transform photo)
    {
        // ��������λ��������ѭ��ʹ��λ�ã�
        int gridIndex = totalPhotosTaken % maxPhotos;

        // ��ȡ��������λ��
        Vector3 basePosition = gridPositions[gridIndex % gridPositions.Count];

        // Ӧ�����ƫ��
        Vector3 randomOffset = new Vector3(
            Random.Range(-maxOffset, maxOffset),
            Random.Range(-maxOffset, maxOffset),
            0
        );

        // Ӧ�������ת
        float randomRotation = Random.Range(-maxRotation, maxRotation);

        // ת����ǽ��ռ�
        Vector3 finalPosition = photoWall.TransformPoint(basePosition + randomOffset);
        Quaternion finalRotation = photoWall.rotation * Quaternion.Euler(0, 0, randomRotation);

        // ����λ�ú���ת
        photo.position = finalPosition;
        photo.rotation = finalRotation;

        // �ط��߷�����΢ƫ�Ʊ����ص�
        photo.position += photoWall.forward * photoForwardOffset;
    }

    void ManagePhotoList(GameObject newPhoto)
    {
        // �����Ƭ�����������ֵ
        if (photos.Count >= maxPhotos)
        {
            // ����Ҫ�滻����Ƭ����
            int replaceIndex = totalPhotosTaken % maxPhotos;

            // ȷ����������Ч��Χ��
            if (replaceIndex < photos.Count && photos[replaceIndex] != null)
            {
                // ���پ���Ƭ
                Destroy(photos[replaceIndex]);

                // �滻Ϊ����Ƭ
                photos[replaceIndex] = newPhoto;
                return;
            }
        }

        // ���δ�ﵽ���ޣ�ֱ����ӵ��б�
        photos.Add(newPhoto);
    }
}