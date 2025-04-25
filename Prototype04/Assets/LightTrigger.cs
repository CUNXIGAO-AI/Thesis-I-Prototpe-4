using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTrigger : MonoBehaviour
{
    // Start is called before the first frame update
 [System.Serializable]
    public class LightConfig
    {
        public Light lightComponent;
        [Tooltip("如果为true，则使用灯光组件当前的intensity作为默认值")]
        public bool useCurrentAsDefault = true;
        [Tooltip("当useCurrentAsDefault为false时使用此值")]
        public float defaultIntensity = 0.2f;
        public float targetIntensity = 2.0f;
        [Tooltip("单独设置此灯光的过渡时间，若为0则使用全局设置")]
        public float customTransitionDuration = 0f;
        [HideInInspector]
        public Coroutine currentCoroutine;
        [HideInInspector]
        public float runtimeDefaultIntensity;
    }
    
    [Tooltip("要控制的灯光列表")]
    public List<LightConfig> lights = new List<LightConfig>();
    
    [Tooltip("所有灯光的默认过渡时间（秒）")]
    public float globalTransitionDuration = 1.5f;
    
    [Tooltip("要检测的玩家标签")]
    private string playerTag = "Animal";
    
    void Awake()
    {
        // 初始化所有灯光设置
        foreach (LightConfig lightConfig in lights)
        {
            if (lightConfig.lightComponent != null)
            {
                // 如果设置为使用当前亮度作为默认值，则保存当前亮度
                if (lightConfig.useCurrentAsDefault)
                {
                    lightConfig.runtimeDefaultIntensity = lightConfig.lightComponent.intensity;
                }
                else
                {
                    // 否则使用指定的默认亮度
                    lightConfig.runtimeDefaultIntensity = lightConfig.defaultIntensity;
                    lightConfig.lightComponent.intensity = lightConfig.defaultIntensity;
                }
            }
        }
    }
    
    void Start()
    {
        // 验证触发器设置
        Collider triggerZone = GetComponentInChildren<Collider>();
        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider>();
        }
        
        if (triggerZone == null)
        {
            Debug.LogError("未找到触发器Collider组件，请确保在此对象或其子对象上添加了碰撞体并设置为Trigger。");
        }
        else if (!triggerZone.isTrigger)
        {
            Debug.LogWarning("检测到的碰撞体没有设置为触发器模式，已自动调整。");
            triggerZone.isTrigger = true;
        }
    }
    
    // 当有对象进入触发区域时
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            foreach (LightConfig lightConfig in lights)
            {
                if (lightConfig.lightComponent != null)
                {
                    if (lightConfig.currentCoroutine != null)
                    {
                        StopCoroutine(lightConfig.currentCoroutine);
                    }
                    
                    float transitionTime = (lightConfig.customTransitionDuration > 0) 
                        ? lightConfig.customTransitionDuration 
                        : globalTransitionDuration;
                    
                    lightConfig.currentCoroutine = StartCoroutine(
                        TransitionLightIntensity(
                            lightConfig.lightComponent, 
                            lightConfig.lightComponent.intensity, 
                            lightConfig.targetIntensity, 
                            transitionTime
                        )
                    );
                }
            }
        }
    }
    
    // 当对象离开触发区域时
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            foreach (LightConfig lightConfig in lights)
            {
                if (lightConfig.lightComponent != null)
                {
                    if (lightConfig.currentCoroutine != null)
                    {
                        StopCoroutine(lightConfig.currentCoroutine);
                    }
                    
                    float transitionTime = (lightConfig.customTransitionDuration > 0) 
                        ? lightConfig.customTransitionDuration 
                        : globalTransitionDuration;
                    
                    lightConfig.currentCoroutine = StartCoroutine(
                        TransitionLightIntensity(
                            lightConfig.lightComponent, 
                            lightConfig.lightComponent.intensity, 
                            lightConfig.runtimeDefaultIntensity, 
                            transitionTime
                        )
                    );
                }
            }
        }
    }
    
    // 协程：平滑过渡灯光亮度
    private IEnumerator TransitionLightIntensity(Light light, float startValue, float endValue, float duration)
    {
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            t = t * t * (3f - 2f * t); // 平滑曲线
            light.intensity = Mathf.Lerp(startValue, endValue, t);
            yield return null;
        }
        
        light.intensity = endValue;
    }
}
