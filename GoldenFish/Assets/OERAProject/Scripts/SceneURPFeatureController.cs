using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SceneURPFeatureController : MonoBehaviour
{
    [Header("URP Renderer ����")]
    public UniversalRendererData rendererData;

    [Header("��������Ҫ�� Features")]
    public string[] featuresToEnable;

    [Header("��Ҫ���õ� RenderObjects ����")]
    public string[] renderObjectsToEnable;

    [Header("����ģʽ")]
    public bool disableOtherFeatures = true;
    public bool disableOtherRenderObjects = true;

    void Start()
    {
        if (rendererData == null)
        {
            Debug.LogError("δ���� URP Renderer Data��", this);
            return;
        }
        ApplyFeatures();
    }

    public void ApplyFeatures()
    {
        // ������ͨ����
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;

            bool shouldEnable = System.Array.Exists(featuresToEnable, name => name == feature.name);
            feature.SetActive(disableOtherFeatures ? shouldEnable : feature.isActive || shouldEnable);
        }

        // ���⴦�� RenderObjects ����
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is RenderObjects renderObjectsFeature)
            {
                bool shouldEnable = System.Array.Exists(renderObjectsToEnable, name => name == renderObjectsFeature.name);
                renderObjectsFeature.SetActive(disableOtherRenderObjects ? shouldEnable : renderObjectsFeature.isActive || shouldEnable);
            }
        }

        rendererData.SetDirty();
        ForcePipelineUpdate();

    }

    private void ForcePipelineUpdate()
    {
        var pipeline = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = null;
        GraphicsSettings.renderPipelineAsset = pipeline;
    }

    [ContextMenu("��ӡ��ǰ���� Features")]
    void PrintFeatures()
    {
        if (rendererData == null) return;

        Debug.Log("=== ��ͨ���� ===");
        foreach (var f in rendererData.rendererFeatures)
            if (f != null && !(f is RenderObjects))
                Debug.Log($"{f.name} : {(f.isActive ? "����" : "����")}");

        Debug.Log("=== RenderObjects ���� ===");
        foreach (var f in rendererData.rendererFeatures)
            if (f is RenderObjects)
                Debug.Log($"{f.name} : {(f.isActive ? "����" : "����")}");
    }
}