using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class memory : MonoBehaviour
{
    [Header("顺序控制")]
    public GameObject[] objectsInOrder; // 依次控制的对象数组
    public GameObject finalObject;      // 最后展示的物体
    public GameObject glass;      // 最后展示的物体
    public GameObject plane;

    [Header("VR设定")]
    public Camera vrCamera;
    public float rayDistance = 100f;
    // public LayerMask raycastLayerMask;

    private int currentIndex = 0;
    private bool sequenceFinished = false;

    private bool canProceed = true;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < objectsInOrder.Length; i++)
        {
            objectsInOrder[i].SetActive(i == 0);
        }

        if (finalObject != null)
        {
            finalObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 如果已经完成序列，就不再处理
        if (sequenceFinished) return;

        


        // 获取左手控制器
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed && canProceed)
        {
            Ray ray = new Ray(vrCamera.transform.position, vrCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green);
            

            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    Debug.Log("Hit object: " + hit.collider.name + " | Tag: " + hit.collider.tag);

                    if (hit.collider.CompareTag("memory"))
                    {
                        Debug.Log("✅ memory seen!");
                        Proceed();
                        canProceed = false; // 防止重复调用
                        break;
                    }
                }
            }

            
        }
        
    }

    public void Proceed()
    {
        if (sequenceFinished) return;
        Debug.Log("captured");

        StartCoroutine(ProceedAfterDelay());
    }

    private IEnumerator ProceedAfterDelay()
    {
        // 延迟一帧
        yield return null;

        // 当前对象隐藏
        objectsInOrder[currentIndex].SetActive(false);
        currentIndex++;

        if (currentIndex < objectsInOrder.Length)
        {
            // 下一个对象激活
            objectsInOrder[currentIndex].SetActive(true);
        }
        else
        {
            // 所有都处理完了，显示最终对象
            if (finalObject != null)
            {
                glass.SetActive(true);
                finalObject.SetActive(true);
                plane.SetActive(false);
            }

            sequenceFinished = true;
        }

        // 给与足够的时间间隔再允许下一次交互
        StartCoroutine(AllowProceedDelay());
    }

        
    private IEnumerator AllowProceedDelay()
    {
        yield return new WaitForSeconds(0.5f); // 0.5秒后可以进行下一次交互
        canProceed = true;
    }
}
