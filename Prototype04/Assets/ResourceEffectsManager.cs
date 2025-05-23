using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ResourceEffectsManager : MonoBehaviour
{
    [System.Serializable]
    public class LightEffect
    {
        public Light targetLight;
        public float fadeDelay = 0.0f;
        public float fadeDuration = 2.0f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        private float initialIntensity;

        public void Initialize()
        {
            if (targetLight != null)
                initialIntensity = targetLight.intensity;
        }

        public float GetInitialIntensity()
        {
            return initialIntensity;
        }
    }
    
    [System.Serializable]
    public class WaterStreamEffect
    {
        public GameObject waterStream;
        public float fadeDelay = 0.0f;
        public float fadeDuration = 1.5f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        private Vector3 initialScale;
        private Vector3 initialPosition;
        private float bottomLocalY;

        public void Initialize()
        {
            if (waterStream != null)
            {
                initialScale = waterStream.transform.localScale;
                initialPosition = waterStream.transform.localPosition;
                bottomLocalY = initialPosition.y - (initialScale.y / 2);
            }
        }

        public Vector3 GetInitialScale()
        {
            return initialScale;
        }

        public Vector3 GetInitialPosition()
        {
            return initialPosition;
        }

        public float GetBottomLocalY()
        {
            return bottomLocalY;
        }
    }
    
    [System.Serializable]
    public class LensFlareEffect
    {
        public LensFlareComponentSRP lensFlare;
        public float fadeDelay = 0.0f;
        public float fadeDuration = 4.0f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        private float initialIntensity;

        public void Initialize()
        {
            if (lensFlare != null)
                initialIntensity = lensFlare.intensity;
        }

        public float GetInitialIntensity()
        {
            return initialIntensity;
        }
    }
    
    [Header("光源效果")]
    public List<LightEffect> lightEffects = new List<LightEffect>();
    
    [Header("水流效果")]
    public List<WaterStreamEffect> waterEffects = new List<WaterStreamEffect>();
    
    [Header("镜头光晕效果")]
    public List<LensFlareEffect> lensFlareEffects = new List<LensFlareEffect>();

    private void Start()
    {
        // 初始化所有效果
        foreach (var effect in lightEffects)
        {
            effect.Initialize();
        }

        foreach (var effect in waterEffects)
        {
            effect.Initialize();
        }

        foreach (var effect in lensFlareEffects)
        {
            effect.Initialize();
        }
    }
    
    public void TriggerAllEffects(float snapDelay)
    {
        // 触发所有光源效果
        foreach (var effect in lightEffects)
        {
            if (effect.targetLight != null)
                StartCoroutine(FadeLight(effect, snapDelay));
        }
        
        // 触发所有水流效果
        foreach (var effect in waterEffects)
        {
            if (effect.waterStream != null)
                StartCoroutine(ScaleWaterStream(effect, snapDelay));
        }
        
        // 触发所有镜头光晕效果
        foreach (var effect in lensFlareEffects)
        {
            if (effect.lensFlare != null)
                StartCoroutine(FadeLensFlare(effect, snapDelay));
        }
    }
    
    // 光源淡出效果协程
    private IEnumerator FadeLight(LightEffect effect, float snapCompletionDelay)
    {
        // 等待snap完成
        yield return new WaitForSeconds(snapCompletionDelay);
        
        // 再等待额外的延迟时间
        yield return new WaitForSeconds(effect.fadeDelay);
        
        // 记录初始亮度
        float initialIntensity = effect.GetInitialIntensity();
        float timer = 0f;
        
        while (timer < effect.fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / effect.fadeDuration;
            float curveValue = effect.fadeCurve.Evaluate(normalizedTime);
            
            // 使用曲线控制亮度
            effect.targetLight.intensity = initialIntensity * (1f - curveValue);
            
            yield return null;
        }
        
        // 确保最终亮度为0
        effect.targetLight.intensity = 0f;
    }

    // 水流缩放效果协程
    private IEnumerator ScaleWaterStream(WaterStreamEffect effect, float snapCompletionDelay)
    {
        yield return new WaitForSeconds(snapCompletionDelay);
        yield return new WaitForSeconds(effect.fadeDelay);

        Vector3 initialScale = effect.GetInitialScale();
        Vector3 initialPosition = effect.GetInitialPosition();
        float bottomLocalY = effect.GetBottomLocalY();
        
        float timer = 0f;

        while (timer < effect.fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(timer / effect.fadeDuration);
            float curveValue = effect.fadeCurve.Evaluate(normalizedTime);

            // 计算新的Y轴缩放
            float newScaleY = Mathf.Lerp(initialScale.y, 0f, curveValue);
            
            // 应用Y轴缩放，X和Z保持不变
            effect.waterStream.transform.localScale = new Vector3(
                initialScale.x,
                newScaleY,
                initialScale.z
            );
            
            // 重要：计算新的位置，使底部保持不变
            float newY = bottomLocalY + (newScaleY / 2);
            effect.waterStream.transform.localPosition = new Vector3(
                initialPosition.x,
                newY,
                initialPosition.z
            );

            yield return null;
        }

        // 确保最后完全收完
        effect.waterStream.transform.localScale = new Vector3(
            initialScale.x,
            0f,
            initialScale.z
        );
        
        // 确保最终位置正确
        effect.waterStream.transform.localPosition = new Vector3(
            initialPosition.x,
            bottomLocalY,
            initialPosition.z
        );
    }

    // 镜头光晕淡出效果协程
    private IEnumerator FadeLensFlare(LensFlareEffect effect, float snapCompletionDelay)
    {
        // 等待snap完成
        yield return new WaitForSeconds(snapCompletionDelay);
        
        // 再等待额外的延迟时间
        yield return new WaitForSeconds(effect.fadeDelay);
        
        // 确保Lens Flare组件存在
        if (effect.lensFlare == null)
        {
            Debug.LogWarning("Lens Flare组件为空，无法淡出");
            yield break;
        }
        
        // 记录初始强度值
        float initialIntensity = effect.GetInitialIntensity();
        
        float timer = 0f;
        
        while (timer < effect.fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(timer / effect.fadeDuration);
            
            // 应用曲线来控制强度
            float intensityValue = effect.fadeCurve.Evaluate(normalizedTime);
            
            // 更新强度
            effect.lensFlare.intensity = Mathf.Lerp(initialIntensity, 0f, intensityValue);
            
            yield return null;
        }
        
        // 确保最终强度为0
        effect.lensFlare.intensity = 0f;
    }

    // 重置所有效果（如果需要重新使用）
    public void ResetAllEffects()
    {
        foreach (var effect in lightEffects)
        {
            if (effect.targetLight != null)
                effect.targetLight.intensity = effect.GetInitialIntensity();
        }

        foreach (var effect in waterEffects)
        {
            if (effect.waterStream != null)
            {
                effect.waterStream.transform.localScale = effect.GetInitialScale();
                effect.waterStream.transform.localPosition = effect.GetInitialPosition();
            }
        }

        foreach (var effect in lensFlareEffects)
        {
            if (effect.lensFlare != null)
                effect.lensFlare.intensity = effect.GetInitialIntensity();
        }
    }
}
