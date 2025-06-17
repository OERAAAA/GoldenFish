using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;

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

    [Header("��Ƭ��������")]
    public bool useCustomAspectRatio = false; // �Ƿ�ʹ���Զ��峤���
    [Range(0.1f, 3f)] public float aspectRatio = 1.7778f; // ��Ƭ����ȣ���/�ߣ���Ĭ��16:9
    public CropMode cropMode = CropMode.FitToWidth; // �ü�ģʽ

    public enum CropMode
    {
        FitToWidth,    // ���ֿ�ȣ��߶�����Ӧ�����ܲü����£�
        FitToHeight,   // ���ָ߶ȣ��������Ӧ�����ܲü����ң�
        Letterbox,    // ������ʾ����Ӻڱߣ�
        Stretch       // ������䣨Ĭ����Ϊ��
    }

    [Header("References")]
    public Transform photoWall;             // ������Ƭ��ƽ��
    public Camera captureCamera;            // ���ڽ�ͼ�����

    private List<GameObject> photos = new List<GameObject>();
    private List<Vector3> gridPositions = new List<Vector3>(); // �洢��������λ��
    private int totalPhotosTaken = 0;       // �ܹ��������Ƭ����
    private bool isLeftTriggerPressed = false;  // ���ְ��״̬
    private bool isRightTriggerPressed = false; // ���ְ��״̬
    private bool canTakePhoto = true;       // �����Ƿ���������
    private float rowHeight = 0f;           // �иߣ�������Ƭ�������㣩
    private int originalCullingMask;        // ��¼ԭʼ�����culling mask

    [EventRef] public string shot;

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
        // ��ȡ�����ֱ��豸
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // ������ְ��״̬
        bool leftTriggerPressed = false;
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftPressed))
        {
            leftTriggerPressed = leftPressed;
        }

        // ������ְ��״̬
        bool rightTriggerPressed = false;
        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightPressed))
        {
            rightTriggerPressed = rightPressed;
        }

        // ������ְ�������¼�
        if (leftTriggerPressed && !isLeftTriggerPressed && canTakePhoto)
        {
            StartCoroutine(CapturePhoto());
            canTakePhoto = false;
        }

        // ������ְ�������¼�
        if (rightTriggerPressed && !isRightTriggerPressed && canTakePhoto)
        {
            StartCoroutine(CapturePhoto());
            canTakePhoto = false;
        }

        // ����һ����ͷ�ʱ������������
        if (!leftTriggerPressed && !rightTriggerPressed)
        {
            canTakePhoto = true;
        }

        // ���°��״̬
        isLeftTriggerPressed = leftTriggerPressed;
        isRightTriggerPressed = rightTriggerPressed;
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

        // �����Ҫ�������
        if (useCustomAspectRatio)
        {
            photoTexture = ProcessAspectRatio(photoTexture);
        }

        // ������Ƭ����
        CreatePhotoObject(photoTexture);

        RuntimeManager.PlayOneShot(shot);

        // ������Ƭ����
        totalPhotosTaken++;
    }

    Texture2D ProcessAspectRatio(Texture2D originalTexture)
    {
        int originalWidth = originalTexture.width;
        int originalHeight = originalTexture.height;

        float screenAspect = (float)originalWidth / originalHeight;
        float targetAspect = aspectRatio;

        // �����ʹ���Զ��������ʹ����Ļ����
        if (!useCustomAspectRatio)
        {
            targetAspect = screenAspect;
        }

        // ����ü�/�������
        int newWidth, newHeight;
        float scale;
        Rect cropRect;

        switch (cropMode)
        {
            case CropMode.FitToWidth: // ���ֿ�ȣ��ü��߶�
                newWidth = originalWidth;
                newHeight = Mathf.RoundToInt(originalWidth / targetAspect);
                scale = 1f;
                cropRect = new Rect(0, (originalHeight - newHeight) / 2, originalWidth, newHeight);
                break;

            case CropMode.FitToHeight: // ���ָ߶ȣ��ü����
                newWidth = Mathf.RoundToInt(originalHeight * targetAspect);
                newHeight = originalHeight;
                scale = 1f;
                cropRect = new Rect((originalWidth - newWidth) / 2, 0, newWidth, originalHeight);
                break;

            case CropMode.Letterbox: // ������ʾ����Ӻڱߣ�
                if (screenAspect > targetAspect) // ��Ļ����
                {
                    newWidth = Mathf.RoundToInt(originalHeight * targetAspect);
                    newHeight = originalHeight;
                }
                else // ��Ļ����
                {
                    newWidth = originalWidth;
                    newHeight = Mathf.RoundToInt(originalWidth / targetAspect);
                }

                // ��������������ɫ
                Texture2D letterboxTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
                Color[] blackPixels = new Color[newWidth * newHeight];
                for (int i = 0; i < blackPixels.Length; i++)
                {
                    blackPixels[i] = Color.black;
                }
                letterboxTexture.SetPixels(blackPixels);

                // �������λ��
                int pasteX = (newWidth - originalWidth) / 2;
                int pasteY = (newHeight - originalHeight) / 2;

                // ȷ������Ч��Χ��
                pasteX = Mathf.Clamp(pasteX, 0, newWidth - originalWidth);
                pasteY = Mathf.Clamp(pasteY, 0, newHeight - originalHeight);

                // ճ��ԭʼͼ��
                letterboxTexture.SetPixels(pasteX, pasteY, originalWidth, originalHeight, originalTexture.GetPixels());
                letterboxTexture.Apply();

                Destroy(originalTexture); // ����ԭʼ����
                return letterboxTexture;

            case CropMode.Stretch: // ������䣨Ĭ�ϣ�
            default:
                newWidth = originalWidth;
                newHeight = originalHeight;
                cropRect = new Rect(0, 0, originalWidth, originalHeight);
                break;
        }

        // ���ڲü�ģʽ��������������������
        if (cropMode == CropMode.FitToWidth || cropMode == CropMode.FitToHeight)
        {
            // ȷ���ü���������Ч��Χ��
            cropRect.width = Mathf.Min(cropRect.width, originalWidth);
            cropRect.height = Mathf.Min(cropRect.height, originalHeight);
            cropRect.x = Mathf.Clamp(cropRect.x, 0, originalWidth - cropRect.width);
            cropRect.y = Mathf.Clamp(cropRect.y, 0, originalHeight - cropRect.height);

            // ����������
            Texture2D croppedTexture = new Texture2D((int)cropRect.width, (int)cropRect.height, TextureFormat.RGB24, false);
            croppedTexture.SetPixels(originalTexture.GetPixels((int)cropRect.x, (int)cropRect.y, (int)cropRect.width, (int)cropRect.height));
            croppedTexture.Apply();

            Destroy(originalTexture); // ����ԭʼ����
            return croppedTexture;
        }

        // ��������ģʽ��ֱ�ӷ���ԭʼ����
        return originalTexture;
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

        // ����ʵ�ʿ�߱�
        float textureAspect = (float)photoTexture.width / photoTexture.height;

        // ���ò��ʺʹ�С
        photo.GetComponent<Renderer>().material = photoMaterial;
        photo.transform.localScale = new Vector3(photoWidth, photoWidth / textureAspect, 1);

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