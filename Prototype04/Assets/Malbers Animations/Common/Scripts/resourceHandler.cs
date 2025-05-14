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

            [Header("拾取触发设置")]
    [Tooltip("如果启用，则在拾取时触发特效")]
    public bool triggerEffectsOnPickup = false;

    // 跟踪上一帧的拾取状态
    private bool wasPickedUp = false;

    [Tooltip("拾取触发时使用的ResourceZone，可从场景中拖拽")]
    public ResourceZone pickupEffectZone;
    [Tooltip("拾取模式下添加资源的延迟时间(秒)，覆盖ResourceManager中的默认值")]
        // 是否处于snapped状态
    private bool isSnapped = false;
    private ResourceZone triggeredZone;


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
            
            // 如果是拾取模式，也应用snap功能
        if (triggerEffectsOnPickup && pickupEffectZone != null && !resourceManager.isPickedUp)
        {
            // 冻结碰撞，防止物理碰撞但保留交互功能
            Collider col = bottleObject.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (bottleRigidbody != null)
            {
                bottleRigidbody.isKinematic = true;
            }
        }
        }
    }
    
    // 应用snap功能的方法，使代码更清晰
    private void ApplySnap(Vector3 snapPosition)
    {        
        // 禁用Pickable组件，直到玩家交互
        if (pickableComponent != null)
        {
            pickableComponent.SetEnable(false);
        }
        
        // 将物体移动到snap点
        StartCoroutine(SmoothSnapToPoint(bottleObject.transform, snapPosition, initialRotation, snapDuration));
        
        // 启动另一个协程来冻结物体一段时间，然后允许拾取
        StartCoroutine(SimpleFreezeThenEnablePickable(currentZone.freezeDuration));
        
        isSnapped = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        ResourceZone zone = other.GetComponent<ResourceZone>();

        // 如果当前是【拾取触发模式】并且已经snapped，则保持当前状态
        if (triggerEffectsOnPickup && isSnapped) return;

        if (zone != null && !zone.hasTriggered && !resourceManager.isPickedUp)
        {
            zone.hasTriggered = true;
            currentZone = zone; // 记录当前所在的zone
                triggeredZone = zone;  // ✅ 记住触发过的zone


            if (zone.bottleSnapPoint != null && bottleObject != null)
            {
                ApplySnap(zone.bottleSnapPoint.position);
            }

            if (!triggerEffectsOnPickup)
            {
                zone.TriggerEffects(snapDuration); 
                resourceManager.AddResource(zone.resourceValue);
                Debug.Log("Resource added (trigger time): " + zone.resourceValue);
            }
}
    }
    
    private IEnumerator SmoothSnapToPoint(Transform objectTransform, Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        // 如果有Rigidbody，先设置为Kinematic
        
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
        // 首先等待物品移动结束
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
            // 在拾取模式下保持snapped状态
            if (!triggerEffectsOnPickup || !isSnapped)
            {
                // 恢复非kinematic状态
                if (bottleRigidbody != null)
                {
                    bottleRigidbody.isKinematic = false;
                }
            }
            
            currentZone = null; // 清除当前zone引用
        }
    }
    
    private void Update()
    {
        // 如果瓶子被拾起，就取消snapped状态
        if (resourceManager.isPickedUp)
        {
            if (isSnapped)
            {
                // 瓶子被拾起，取消snapped状态
                isSnapped = false;
                
                // 恢复非kinematic状态
                if (bottleRigidbody != null)
                {
                    bottleRigidbody.isKinematic = false;
                }
            }
            
            if (triggeredZone != null)
            {
                triggeredZone.NotifyPickupAfterTrigger();
                triggeredZone = null;  // ✅ 确保只激活一次
            }
        }
        
        // 修改拾取触发逻辑
        if (triggerEffectsOnPickup && pickupEffectZone != null)
        {
            // 检测拾取状态的变化 - 从未拾取变为已拾取
        if (resourceManager.isPickedUp && !wasPickedUp)
        {
            // ✅ 恢复碰撞体
            Collider col = bottleObject.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false; // 允许物理碰撞
            }

            // ✅ 恢复刚体
            if (bottleRigidbody != null)
            {
                bottleRigidbody.isKinematic = false;
            }

            // ✅ 保留原本的特效触发逻辑
            pickupEffectZone.TriggerEffects(0f);
            StartCoroutine(resourceManager.DelayedPickupResource(pickupEffectZone.resourceValue));
            triggerEffectsOnPickup = false;

            Debug.Log("拾取触发 - 已触发特效，使用拾取专用延迟添加资源");
                        if (currentZone != null)
            {
                currentZone.NotifyPickupAfterTrigger();
            }
        }
        }
        // 更新上一帧的拾取状态
        wasPickedUp = resourceManager.isPickedUp;
        
        // 更新调试状态
        if (pickableComponent != null)
        {
            isPickableNow = pickableComponent.enabled;
        }
    }
}
}