using System.Collections;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Debug")]
    public string currentLightStateDebug = "Default";

    [SerializeField, Tooltip("当前显示亮度值，仅用于调试")]
    private float currentDisplayValueDebug;

    [SerializeField]
    private bool isDepleting = false;     // 标记资源是否正在消耗

    [Header("资源设置")]
    public float currentValue = 100f;    // 当前资源值
    public float maxValue = 100f;        // 最大资源值
    public float depletionRate = 5f;     // 消耗率 
    public float depletionMultiplier = 1f; // 消耗倍数
    
    [Header("光线设置")]
    public Light resourceLight;          // 引用场景中的灯光组件
    public float minIntensity = 0.1f;    // 最小亮度（资源为0时）
    public float maxIntensity = 3.0f;    // 最大亮度（资源满时）
    
    [Header("时间延迟类设置")]
    [SerializeField] private float resourceAddDelay = 0.5f;  // 水瓶丢进去后多久获得资源（开始发光）
    [SerializeField] private float brightnessIncreaseSpeed = 1.0f;  // 亮度增加速度
    public AnimationCurve brightnessIncreaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 默认曲线
    [SerializeField] private float brightnessDecreaseSpeed = 1.0f;  // 亮度减少速度
    public AnimationCurve brightnessDecreaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 亮度减少曲线

        // 用于亮度增加的时间追踪
    private float increaseTimer = 0f;
    private float decreaseTimer = 0f; // 新增：用于减少效果的计时器
    private float startValue = 0f;
    private float targetValue = 0f;
    private bool isIncreasing = false;
    private bool isDecreasing = false; // 新增：标记是否正在减少
    
    
    //[Header("UI Settings")]             // 原UI设置的引用（保留但不直接使用）
    //public Image WaterdropImage;
    //public TextMeshProUGUI WaterdropText;

    private bool isInitialized = false;   // 标记资源是否已初始化
    public bool isPickedUp { get; private set; } = false;
    
    private float currentDisplayValue;    // 当前显示值（用于平滑过渡）

    public event System.Action OnResourceDepleted;

    [Header("对冲效果")]
public bool enableOvershoot = true;        // 是否启用过冲效果
public float overshootPercent = 50f;       // 过冲百分比(比如50表示会超过目标值50%)
public AnimationCurve overshootCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 过冲曲线
public float overshootThreshold = 0.75f;   // 过冲触发的阈值（0-1之间）
private bool isOvershooting = false;       // 当前是否在过冲状态
private float overshootTarget;             // 过冲目标值
private float overshootTimer = 0f;         // 过冲计时器
private bool isReadyForOvershoot = false;  // 是否准备好执行过冲效果


        [Header("闪烁效果")]
    public float defaultFlickerFrequency = 1.0f;     // 默认呼吸式闪烁速度
    public float defaultFlickerIntensity = 0.1f; // 默认呼吸式闪烁强度

    public float detectedFlickerFrequency = 5.0f;    // 被检测时闪烁速度
    public float detectedFlickerIntensity = 0.3f; // 被检测时闪烁强度

    public float combatFlickerFrequency = 10.0f;     // 战斗状态闪烁速度
    public float combatFlickerIntensity = 0.5f;  // 战斗状态闪烁强度

    private float currentFlickerFrequency;          // 当前闪烁速度
    private float currentFlickerIntensity;      // 当前闪烁强度
    private float flickerTime = 0.0f;           // 闪烁计时器
    private LightState currentLightState = LightState.Default; // 当前灯光状态

    [Header("资源归零过渡设置")]
    [Tooltip("当资源归0瓶子到熄灭时的过渡")]
    public float transitionDuration = 3.5f;     // 过渡持续时间（秒）
    public float transitionDelay = 1f;       // 过渡前的延迟时间

    


    public enum LightState
    {
        Default,
        Detected,
        Combat
    }

    private void Awake()
    {
        // 隐藏原UI元素
        /*
        if (WaterdropImage != null)
        {
            var tempColor = WaterdropImage.color;
            tempColor.a = 0;
            WaterdropImage.color = tempColor;
        }

        if (WaterdropText != null)
        {
            var tempColor = WaterdropText.color;
            tempColor.a = 0;
            WaterdropText.color = tempColor;
        }*/
        
        // 初始化显示值
        currentDisplayValue = currentValue;
        UpdateLightIntensity();

        currentFlickerFrequency = defaultFlickerFrequency;
        currentFlickerIntensity = defaultFlickerIntensity;
        
    }

 private void Update()
{
    // 处理资源消耗
    if (isDepleting && currentValue > 0) 
    {
        currentValue -= depletionRate * depletionMultiplier * Time.deltaTime;

        if (currentValue <= 0)
        {
            currentValue = 0;
            isDepleting = false;
            OnResourceDepleted?.Invoke();
            
            // 资源耗尽时启动过渡协程
            StartCoroutine(TransitionToDefaultFlicker());
        }
    }
    
    // 处理显示值更新
    if (isOvershooting)
    {
        // 如果在过冲状态，执行过冲更新
        UpdateOvershoot();
    }
    else
    {
        // 平滑过渡逻辑
        if (currentValue > currentDisplayValue)
        {
            // 如果之前在减少状态，重置减少状态
            if (isDecreasing)
            {
                isDecreasing = false;
            }
            
            // 如果不在增加状态，开始一个新的增加过程
            if (!isIncreasing)
            {
                isIncreasing = true;
                increaseTimer = 0f;
                startValue = currentDisplayValue;
                targetValue = currentValue;
            }
            
            // 更新计时器
            increaseTimer += Time.deltaTime * brightnessIncreaseSpeed;
            float normalizedTime = Mathf.Clamp01(increaseTimer);
            
            // 使用曲线计算当前值
            currentDisplayValue = Mathf.Lerp(startValue, targetValue, 
                brightnessIncreaseCurve.Evaluate(normalizedTime));
            
            // 如果完成了增加过程
            if (normalizedTime >= 1.0f)
            {
                isIncreasing = false;
                currentDisplayValue = targetValue;
            }
        }
        else if (currentValue < currentDisplayValue)
        {
            // 如果之前在增加状态，重置增加状态
            if (isIncreasing)
            {
                isIncreasing = false;
            }
            
            // 如果不在减少状态，开始一个新的减少过程
            if (!isDecreasing)
            {
                isDecreasing = true;
                decreaseTimer = 0f;
                startValue = currentDisplayValue;
                targetValue = currentValue;
            }
            
            // 更新计时器
            decreaseTimer += Time.deltaTime * brightnessDecreaseSpeed;
            float normalizedTime = Mathf.Clamp01(decreaseTimer);
            
            // 使用曲线计算当前值
            currentDisplayValue = Mathf.Lerp(startValue, targetValue, 
                brightnessDecreaseCurve.Evaluate(normalizedTime));
            
            // 如果完成了减少过程
            if (normalizedTime >= 1.0f)
            {
                isDecreasing = false;
                currentDisplayValue = targetValue;
            }
        }
        else
        {
            // 如果当前值等于显示值，重置所有状态
            isIncreasing = false;
            isDecreasing = false;
        }
        
        // 检测过冲条件
        if (currentValue > 0)
        {
            float percentageReached = currentDisplayValue / currentValue;
            
            // 只有当满足以下所有条件时才触发过冲
            if (enableOvershoot && 
                !isReadyForOvershoot && 
                !isOvershooting &&  // 确保当前没有在过冲状态
                currentValue > startValue && // 只在资源增加时
                Mathf.Abs(currentValue - currentDisplayValue) > 0.01f && // 确保有足够的差距
                percentageReached >= overshootThreshold)
            {
                isReadyForOvershoot = true;
                StartOvershoot();
            }
}
    }
        
    UpdateLightIntensity();
}

private void StartOvershoot()
{
    isOvershooting = true;
    overshootTarget = currentValue + (currentValue * (overshootPercent / 100f));
    overshootTimer = 0f;
}

private void UpdateOvershoot()
{
    // 增加计时器，不限制在0-1范围内
    overshootTimer += Time.deltaTime;
    
    // 获取曲线的时间范围
    float curveDuration = overshootCurve[overshootCurve.length - 1].time;
    
    // 使用动画曲线的值来确定当前显示值
    float curveValue = overshootCurve.Evaluate(overshootTimer);
    
    // 计算当前显示值
    currentDisplayValue = Mathf.Lerp(currentValue, overshootTarget, curveValue);
    
    // 如果完成整个曲线的播放，重置状态
    if (overshootTimer >= curveDuration)
    {
        currentDisplayValue = currentValue;
        isOvershooting = false;
        isReadyForOvershoot = false;  // 重置过冲准备状态，允许再次触发
        overshootTimer = 0f;
    }
}

        private IEnumerator TransitionToDefaultFlicker()
    {
        // 等待延迟时间
        yield return new WaitForSeconds(transitionDelay);
        
        // 保存开始时的闪烁速度
        float startSpeed = currentFlickerFrequency;
        float elapsedTime = 0f;
        
        // 在过渡持续时间内平滑过渡
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);
            
            // 平滑过渡闪烁速度
            currentFlickerFrequency = Mathf.Lerp(startSpeed, defaultFlickerFrequency, t);
            
            yield return null;
        }
        
        // 确保最终值精确
        currentFlickerFrequency = defaultFlickerFrequency;
        
        // 确保状态也设置为默认
        SetLightState(LightState.Default);
    }
        
    // 更新灯光亮度的方法
    private void UpdateLightIntensity()
    {
        if (resourceLight != null)
        {
            // 计算基础亮度
            float valuePercentage = Mathf.Clamp01(currentDisplayValue / maxValue);
            float baseIntensity = Mathf.Lerp(minIntensity, maxIntensity, valuePercentage);
            
            // 应用闪烁效果
            flickerTime += Time.deltaTime * currentFlickerFrequency;
            float flickerFactor = Mathf.Sin(flickerTime) * currentFlickerIntensity;
            resourceLight.intensity = baseIntensity * (1.0f + flickerFactor);
            
            // 更新调试变量 - 只显示当前显示值
            currentDisplayValueDebug = currentDisplayValue;
        }
    }


public void SetLightState(LightState state)
{
    currentLightState = state;
    
    // 更新调试显示
    switch(state)
    {
        case LightState.Default:
            currentLightStateDebug = "Default";
            break;
        case LightState.Detected:
            currentLightStateDebug = "Detected";
            break;
        case LightState.Combat:
            currentLightStateDebug = "Combat";
            break;
    }
    
    // 根据状态设置闪烁参数
    switch (state)
    {
        case LightState.Default:
            currentFlickerFrequency = defaultFlickerFrequency;
            currentFlickerIntensity = defaultFlickerIntensity;
            break;
            
        case LightState.Detected:
            currentFlickerFrequency = detectedFlickerFrequency;
            currentFlickerIntensity = detectedFlickerIntensity;
            break;
            
        case LightState.Combat:
            currentFlickerFrequency = combatFlickerFrequency;
            currentFlickerIntensity = combatFlickerIntensity;
            break;
    }
}

    public void StartResourceDepletion(float initialMaxValue)
    {
        if (!isInitialized)
        {
            //maxValue = initialMaxValue;
            //currentValue = maxValue;
            isInitialized = true;
        }
            isDepleting = true;
    }

    public void StopResourceDepletion()
    {
        isDepleting = false;
    }

    public void SetDepletionMultiplier(float multiplier)
    {
        depletionMultiplier = multiplier;
    }

    public void SetDepletionRate(float rate)
    {
        depletionRate = rate;
    }

    public void TriggerResourceDepleted() 
    {
        OnResourceDepleted?.Invoke();
    }

    public void IsPickedUp()
    {
        isPickedUp = true;
        // Debug.Log("物品已拾取!");
    }

    public void IsDroppedBy()
    {
        isPickedUp = false;
        // Debug.Log("物品已丢弃!");
    }

    public void AddResource(float amount)
    {
        isOvershooting = false;
        isReadyForOvershoot = false;
        isIncreasing = false; // 重置增加状态
        StartCoroutine(DelayedAddResource(amount));
    }
        
    // 延迟添加资源
    private IEnumerator DelayedAddResource(float amount)
    {
        // 添加资源前的延迟
        yield return new WaitForSeconds(resourceAddDelay);
        
        bool wasEmpty = currentValue <= 0;
        
        // 直接修改资源值，显示过渡由Update处理
        currentValue = Mathf.Min(currentValue + amount, maxValue);
        
        // 如果之前资源为空，现在有了资源，重置光的状态
        if (wasEmpty && currentValue > 0)
        {
            SetLightState(LightState.Default);
        }
    }

}


#region 
    // ========================
    // 以下是原UI相关的注释代码
    // ========================
    
    /*
    public void StartUITransition()
    {
        if (WaterdropImage != null)
        {
            StartCoroutine(PlayUITransition());
        }
    }

    private IEnumerator PlayUITransition()
    {
        float elapsedTime = 0f;

        // 淡入WaterdropImage
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetImageAlpha(WaterdropImage, alpha);
            yield return null;
        }

        // 图像完全可见的状态持续1秒
        yield return new WaitForSeconds(1f);

        // 淡出WaterdropImage
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (elapsedTime / fadeDuration));
            SetImageAlpha(WaterdropImage, alpha);
            yield return null;
        }

        // 过渡后开始资源消耗
        StartResourceDepletion(currentValue);

        // 资源消耗开始后显示WaterdropText，带有淡入效果
        if (WaterdropText != null)
        {
            StartCoroutine(FadeText(WaterdropText, 0f, 1f, fadeDuration));
        }
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color tempColor = text.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            tempColor.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            text.color = tempColor;
            yield return null;
        }

        tempColor.a = endAlpha;
        text.color = tempColor;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            var tempColor = image.color;
            tempColor.a = alpha;
            image.color = tempColor;
        }
    }
    
    // UI文本更新逻辑
    // if (WaterdropText.color.a > 0)
    // {
    //     WaterdropText.text = currentValue.ToString("F0");
    // }
    */
#endregion