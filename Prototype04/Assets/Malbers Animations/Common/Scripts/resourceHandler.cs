using System.Collections;
using UnityEngine.Rendering;
using UnityEngine;

namespace MalbersAnimations.Controller
{
public class ResourceHandler : MonoBehaviour
{  [Header("调试用")]
        public bool isPickableNow;
        
        private ResourceManager resourceManager;
        
        // 通过Inspector拖拽
        public GameObject bottleObject;
        
        // 瓶子的Pickable组件(通过Inspector拖拽)
        public Pickable pickableComponent;
        
        // 保存初始旋转状态
        private Quaternion initialRotation;
        
        // 用于检测是否在zone内
        private ResourceZone currentZone;
        
        // 瓶子的Rigidbody组件
        private Rigidbody bottleRigidbody;
        
        [Header("平滑移动设置")]
        public float snapDuration = 0.5f; // 移动持续时间
        public AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 移动曲线
        
        private void Start()
        {
            resourceManager = GetComponentInChildren<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("找不到ResourceManager!");
            }
            
            // 保存物品的初始旋转状态
            if (bottleObject != null)
            {
                initialRotation = bottleObject.transform.rotation;
                bottleRigidbody = bottleObject.GetComponent<Rigidbody>();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            ResourceZone zone = other.GetComponent<ResourceZone>();
            if (zone != null && !zone.hasTriggered && !resourceManager.isPickedUp)
            {
                zone.hasTriggered = true;
                currentZone = zone; // 记录当前所在的zone
                
                // 如果定义了吸附点，则将瓶子平滑移动到吸附点
                if (zone.bottleSnapPoint != null && bottleObject != null)
                {
                    // 禁用Pickable组件使物品不可拾取
                    if (pickableComponent != null)
                    {
                        pickableComponent.SetEnable(false);
                    }
                    
                    // 启动平滑移动协程
                    StartCoroutine(SmoothSnapToPoint(bottleObject.transform, zone.bottleSnapPoint.position, initialRotation, snapDuration));
                    
                    // 启动冻结定时器
                    StartCoroutine(SimpleFreezeThenEnablePickable(zone.freezeDuration));
                    
                    // 触发区域特定效果
                    zone.TriggerEffects(snapDuration);
                }
                
                resourceManager.AddResource(zone.resourceValue);
                Debug.Log("Resource added: " + zone.resourceValue);
            }
        }
        
        private IEnumerator SmoothSnapToPoint(Transform objectTransform, Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            // 如果有Rigidbody，先设置为Kinematic
            if (bottleRigidbody != null)
            {
                bottleRigidbody.isKinematic = true;
            }
            
            Vector3 startPosition = objectTransform.position;
            Quaternion startRotation = objectTransform.rotation;
            
            float elapsedTime = 0;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;
                float curveValue = snapCurve.Evaluate(normalizedTime);
                
                // 平滑移动位置
                objectTransform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                
                // 平滑旋转
                objectTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
                
                yield return null;
            }
            
            // 确保最终位置和旋转精确匹配目标值
            objectTransform.position = targetPosition;
            objectTransform.rotation = targetRotation;
            
            // 完成移动后保持Kinematic状态
            if (bottleRigidbody != null)
            {
                bottleRigidbody.isKinematic = true;
            }
        }
        
        private IEnumerator SimpleFreezeThenEnablePickable(float freezeDuration)
        {
            // 首先等待物品移动结束（使用与snapDuration相同的时间或略长一些）
            yield return new WaitForSeconds(snapDuration + 0.1f);
            
            // 然后等待额外的冻结时间
            yield return new WaitForSeconds(freezeDuration);
            
            Debug.Log("冻结时间结束 - 物品可以被拾取");
            
            // 启用Pickable组件
            if (pickableComponent != null)
            {
                pickableComponent.SetEnable(true);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            ResourceZone zone = other.GetComponent<ResourceZone>();
            if (zone != null && zone == currentZone) // 确保是同一个zone
            {
                // 恢复非kinematic状态
                if (bottleRigidbody != null)
                {
                    bottleRigidbody.isKinematic = false;
                }
                
                currentZone = null; // 清除当前zone引用
            }
        }
        
        // 如果瓶子被拾起，也应该重置zone的状态
        private void Update()
        {
            if (resourceManager.isPickedUp && currentZone != null)
            {
                // 瓶子被拾起，重置zone状态
                currentZone = null;
            }
            
            // 更新调试状态
            if (pickableComponent != null)
            {
                isPickableNow = pickableComponent.enabled;
            }
        }
    }
}