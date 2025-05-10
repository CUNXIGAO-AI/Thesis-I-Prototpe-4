using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightTrigger : MonoBehaviour
{
    public float Delay = 0.5f; // 延迟时间（秒）

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
            [Tooltip("如果为true，此灯光只会被触发一次")]
    public bool oneTimeOnly = false;
    [HideInInspector]
    public bool hasBeenTriggered = false;
    }

    [System.Serializable]
    public class LensFlareConfig
    {
        public LensFlareComponentSRP lensFlareComponent;
        [Tooltip("如果为true，则使用光晕组件当前的intensity作为默认值")]
        public bool useCurrentAsDefault = true;
        [Tooltip("当useCurrentAsDefault为false时使用此值")]
        public float defaultIntensity = 0.2f;
        public float targetIntensity = 2.0f;
        [Tooltip("单独设置此光晕的过渡时间，若为0则使用全局设置")]
        public float customTransitionDuration = 0f;
        [HideInInspector]
        public Coroutine currentCoroutine;
        [HideInInspector]
        public float runtimeDefaultIntensity;
         [Tooltip("如果为true，此光晕只会被触发一次")]
    public bool oneTimeOnly = false;
    [HideInInspector]
    public bool hasBeenTriggered = false;

    }
    
    
    [Tooltip("要控制的灯光列表")]
    public List<LightConfig> lights = new List<LightConfig>();
    
    [Tooltip("所有灯光的默认过渡时间（秒）")]
    public float globalTransitionDuration = 1.5f;
    

        [Tooltip("要控制的镜头光晕列表")]
    public List<LensFlareConfig> lensFlares = new List<LensFlareConfig>();

        [Header("水面效果设置")]
    [Tooltip("是否对水面接触事件做出反应")]
    public bool reactToWaterContact = false;
    [Tooltip("水面效果触发半径")]
    public float waterEffectRadius = 10f;
    
    // 灯光是否当前已被激活(由触发器或水面效果)
    private bool lightsActivated = false;

    void Awake()
{
    // 初始化所有灯光设置
    foreach (LightConfig lightConfig in lights)
    {
        if (lightConfig.lightComponent != null)
        {
            // 原有代码保持不变
            if (lightConfig.useCurrentAsDefault)
            {
                lightConfig.runtimeDefaultIntensity = lightConfig.lightComponent.intensity;
            }
            else
            {
                lightConfig.runtimeDefaultIntensity = lightConfig.defaultIntensity;
                lightConfig.lightComponent.intensity = lightConfig.defaultIntensity;
            }
        }
    }
    
    // 初始化所有镜头光晕设置
    foreach (LensFlareConfig flareConfig in lensFlares)
    {
        if (flareConfig.lensFlareComponent != null)
        {
            if (flareConfig.useCurrentAsDefault)
            {
                flareConfig.runtimeDefaultIntensity = flareConfig.lensFlareComponent.intensity;
            }
            else
            {
                flareConfig.runtimeDefaultIntensity = flareConfig.defaultIntensity;
                flareConfig.lensFlareComponent.intensity = flareConfig.defaultIntensity;
            }
        }
    }
}

   
    void OnEnable()
    {
        if (reactToWaterContact)
        {
            // 订阅水面接触事件
            WaterEffectEventManager.OnWaterContact += HandleWaterContact;
        }
    }
    
    void OnDisable()
    {
        if (reactToWaterContact)
        {
            // 取消订阅水面接触事件
            WaterEffectEventManager.OnWaterContact -= HandleWaterContact;
        }
    }
    
    // 处理水面接触事件
    private void HandleWaterContact(Vector3 contactPoint)
    {
        if (!reactToWaterContact) return;
        
        // 检查接触点是否在范围内
        float distance = Vector3.Distance(transform.position, contactPoint);
        if (distance <= waterEffectRadius)
        {
            // 如果灯光已经被激活，不需要再次激活
            if (lightsActivated) return;
            
            // 激活灯光
            StartCoroutine(DelayedActivateLights(Delay));        }
    }
    
    // 当有对象进入触发区域时

    private IEnumerator DelayedActivateLights(float delay)
{
    yield return new WaitForSeconds(delay);
    ActivateLights();
}

    
    // 当对象离开触发区域时
    // 激活灯光的方法
    private void ActivateLights()
    {
        lightsActivated = true;
        
        // 处理灯光
        foreach (LightConfig lightConfig in lights)
        {
            if (lightConfig.oneTimeOnly && lightConfig.hasBeenTriggered) continue;

            if (lightConfig.lightComponent != null)
            {
                if (lightConfig.currentCoroutine != null)
                    StopCoroutine(lightConfig.currentCoroutine);

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

                lightConfig.hasBeenTriggered = true;
            }
        }

        // 处理镜头光晕
        foreach (LensFlareConfig flareConfig in lensFlares)
        {
            if (flareConfig.oneTimeOnly && flareConfig.hasBeenTriggered) continue;

            if (flareConfig.lensFlareComponent != null)
            {
                if (flareConfig.currentCoroutine != null)
                    StopCoroutine(flareConfig.currentCoroutine);

                float transitionTime = (flareConfig.customTransitionDuration > 0)
                    ? flareConfig.customTransitionDuration
                    : globalTransitionDuration;

                flareConfig.currentCoroutine = StartCoroutine(
                    TransitionLensFlareIntensity(
                        flareConfig.lensFlareComponent,
                        flareConfig.lensFlareComponent.intensity,
                        flareConfig.targetIntensity,
                        transitionTime
                    )
                );

                flareConfig.hasBeenTriggered = true;
            }
        }
    }
    
    // 停用灯光的方法
    private void DeactivateLights()
    {
        lightsActivated = false;
        
        // 处理灯光
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
        
        // 处理镜头光晕
        foreach (LensFlareConfig flareConfig in lensFlares)
        {
            if (flareConfig.lensFlareComponent != null)
            {
                if (flareConfig.currentCoroutine != null)
                {
                    StopCoroutine(flareConfig.currentCoroutine);
                }
                
                float transitionTime = (flareConfig.customTransitionDuration > 0) 
                    ? flareConfig.customTransitionDuration 
                    : globalTransitionDuration;
                
                flareConfig.currentCoroutine = StartCoroutine(
                    TransitionLensFlareIntensity(
                        flareConfig.lensFlareComponent, 
                        flareConfig.lensFlareComponent.intensity, 
                        flareConfig.runtimeDefaultIntensity, 
                        transitionTime
                    )
                );
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

     private IEnumerator TransitionLensFlareIntensity(LensFlareComponentSRP lensFlare, float startValue, float endValue, float duration)
    {
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            t = t * t * (3f - 2f * t); // 使用与灯光相同的平滑曲线
            lensFlare.intensity = Mathf.Lerp(startValue, endValue, t);
            yield return null;
        }
        
        lensFlare.intensity = endValue;
    }

        void OnDrawGizmosSelected()
    {
        if (reactToWaterContact)
        {
            Gizmos.color = new Color(0, 0.8f, 1, 0.3f);
            Gizmos.DrawSphere(transform.position, waterEffectRadius);
        }
    }
}
