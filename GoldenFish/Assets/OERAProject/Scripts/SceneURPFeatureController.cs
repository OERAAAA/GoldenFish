using UnityEngine;
using UnityEngine.Rendering; // �����������ռ�
using UnityEngine.Rendering.Universal;

public class SceneURPFeatureController : MonoBehaviour
{
    [Header("URP Renderer ����")]
    public UniversalRendererData rendererData;

    [Header("��������Ҫ�� Features")]
    public string[] featuresToEnable;

    [Header("����ģʽ")]
    public bool disableOtherFeatures = true;

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
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;

            bool shouldEnable = System.Array.Exists(featuresToEnable, name => name == feature.name);
            feature.SetActive(disableOtherFeatures ? shouldEnable : feature.isActive || shouldEnable);
        }

        rendererData.SetDirty();
        // �滻ԭ���� GraphicsSettings ˢ�·�ʽ
        ForcePipelineUpdate();
        Debug.Log($"��Ӧ�ó��� {gameObject.scene.name} ������");
    }

    // �µĹ���ˢ�·���
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
        foreach (var f in rendererData.rendererFeatures)
            if (f != null) Debug.Log($"{f.name} : {(f.isActive ? "����" : "����")}");
    }
}