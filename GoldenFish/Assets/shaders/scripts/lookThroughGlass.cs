using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class lookThroughGlass : MonoBehaviour
{
    public Camera vrCamera;             // XR 相机
    public string glassTag = "Glass";   // 玻璃球的 Tag
    public string targetTag = "Target"; // 目标物体的 Tag
    public float maxDistance = 20f;
    public string triggerObjectTag = "Glass"; 
    public float triggerDistance = 0.2f;

    float requiredTime = 1.5f;  // 需要持续的时间，单位秒
    float timeSeeingThroughGlass = 0f;  // 用于计时

    public GameObject frosted;  // 要隐藏的对象
    public GameObject glass;   // 要显示的对象

    public string nextSceneName = "testVR"; 

    bool wasSeeingThroughGlass = false; // 上一帧的状态
    bool hitTarget = false;
    bool hitGlass = false;
    bool isSeeingThroughGlass = false;

    public ParticleSystem particleEffect; // 粒子效果引用

    
    void Start()
    {
        if (particleEffect != null)
        {
            particleEffect.Stop(); // 确保粒子效果一开始是停止状态
             particleEffect.gameObject.SetActive(false);  // 确保一开始粒子系统不可见
            var main = particleEffect.main;  // 获取粒子系统的 main 模块
            main.simulationSpeed = 0.27f;   // 设置播放速度为 0.15
        }
    }
    
    void Update()
    {
        hitGlass = false;
        hitTarget = false;
        // 从相机正前方发出射线
        Ray ray = new Ray(vrCamera.transform.position, vrCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green);

        // 检测所有击中的碰撞体（按距离排序）
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        
        if (hits.Length == 0) return;

        
        foreach (RaycastHit hit in hits)
        {
            
            if (hit.collider.CompareTag(glassTag))
            {
                hitGlass = true;

            }
            else if (hit.collider.CompareTag(targetTag))
            {
                hitTarget = true;


            }
        }

        isSeeingThroughGlass = hitGlass && hitTarget;
        
        // 判断状态是否发生了变化
        if (isSeeingThroughGlass != wasSeeingThroughGlass)
        {
            wasSeeingThroughGlass = isSeeingThroughGlass; // 更新状态

            if (isSeeingThroughGlass)
            {

                if (frosted != null) frosted.SetActive(false);
                if (glass != null) glass.SetActive(true);
                if (particleEffect != null)
                {
                    particleEffect.gameObject.SetActive(true); // 激活粒子效果
                    particleEffect.Play();
                }
            }
            else
            {
                timeSeeingThroughGlass = 0f;

                if (frosted != null) frosted.SetActive(true);
                if (glass != null) glass.SetActive(false);
                if (particleEffect != null)
                {
                    particleEffect.Stop();  // 停止粒子效果
                    particleEffect.gameObject.SetActive(false); // 隐藏粒子效果
                }
            }
        }

        if (isSeeingThroughGlass)
        {

            // 增加计时
            timeSeeingThroughGlass += Time.deltaTime;

            if (timeSeeingThroughGlass >= requiredTime)
            {

                SceneManager.LoadScene(nextSceneName); // 切换场景
            }

            // GameObject targetObject = GameObject.FindGameObjectWithTag(glassTag);
            // if (targetObject != null)
            // {
            //     float distance = Vector3.Distance(vrCamera.transform.position, targetObject.transform.position);
            //     if (distance <= triggerDistance) // 如果距离小于设定的阈值
            //     {
            //         Debug.Log("🌟 目标物体足够近，切换场景！");
            //         SceneManager.LoadScene(nextSceneName); // 记得换成你的目标场景名
            //     }
            // }
        }
    }

    
            
        
}
