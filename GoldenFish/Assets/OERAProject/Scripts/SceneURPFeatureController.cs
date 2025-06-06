using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SceneURPFeatureController : MonoBehaviour
{
    [Header("URP Renderer 配置")]
    public UniversalRendererData rendererData;

    [Header("本场景需要的 Features")]
    public string[] featuresToEnable;

    [Header("需要启用的 RenderObjects 特性")]
    public string[] renderObjectsToEnable;

    [Header("操作模式")]
    public bool disableOtherFeatures = true;
    public bool disableOtherRenderObjects = true;

    void Start()
    {
        if (rendererData == null)
        {
            Debug.LogError("未分配 URP Renderer Data！", this);
            return;
        }
        ApplyFeatures();
    }

    public void ApplyFeatures()
    {
        // 处理普通特性
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;

            bool shouldEnable = System.Array.Exists(featuresToEnable, name => name == feature.name);
            feature.SetActive(disableOtherFeatures ? shouldEnable : feature.isActive || shouldEnable);
        }

        // 特殊处理 RenderObjects 特性
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

    [ContextMenu("打印当前所有 Features")]
    void PrintFeatures()
    {
        if (rendererData == null) return;

        Debug.Log("=== 普通特性 ===");
        foreach (var f in rendererData.rendererFeatures)
            if (f != null && !(f is RenderObjects))
                Debug.Log($"{f.name} : {(f.isActive ? "启用" : "禁用")}");

        Debug.Log("=== RenderObjects 特性 ===");
        foreach (var f in rendererData.rendererFeatures)
            if (f is RenderObjects)
                Debug.Log($"{f.name} : {(f.isActive ? "启用" : "禁用")}");
    }
}