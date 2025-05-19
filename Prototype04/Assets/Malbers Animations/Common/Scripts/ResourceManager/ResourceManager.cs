using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


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

    [Header("镜头光晕组件")]
public LensFlareComponentSRP lensFlareSRP; // 替代 spotlight
public float minFlareIntensity = 0.1f;
public float maxFlareIntensity = 1.0f;

    
    [Header("时间延迟类设置")]
    public float resourceAddDelay = 0.5f;
    [SerializeField] private float brightnessIncreaseSpeed = 1.0f;
    public AnimationCurve brightnessIncreaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float brightnessDecreaseSpeed = 1.0f;
    public AnimationCurve brightnessDecreaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float increaseTimer = 0f;
    private float decreaseTimer = 0f;
    private float startValue = 0f;
    private float targetValue = 0f;
    private bool isIncreasing = false;
    private bool isDecreasing = false;

    private bool isInitialized = false;
    public bool isPickedUp { get; private set; } = false;

    private float currentDisplayValue;
    public event System.Action OnResourceDepleted;

    [Header("闪烁效果")]
    public float defaultFlickerFrequency = 1.0f;
    public float defaultFlickerIntensity = 0.1f;
    public float detectedFlickerFrequency = 5.0f;
    public float detectedFlickerIntensity = 0.3f;
    public float combatFlickerFrequency = 10.0f;
    public float combatFlickerIntensity = 0.5f;

    private float currentFlickerFrequency;
    private float currentFlickerIntensity;
    private float flickerTime = 0.0f;
    private LightState currentLightState = LightState.Default;

    [Header("拾取延迟设置")]
    public float pickupResourceDelay = 0.3f;

    [Header("资源归零过渡设置")]
    public float transitionDuration = 3.5f;
    public float transitionDelay = 1f;
    public float flareTransitionSpeed = 1f;
    private float currentFlareIntensity = 0f;
    [Header("丢弃延迟设置")]
public float dropLightDelay = 0.5f;  // 丢弃后光晕延迟亮起时间
private bool justDropped = false;    // 标记是否刚被丢弃

private bool suppressDropEffects = false;

public bool requireWaterContact = true;

    private bool hasTouchedWater = false;
private bool isFlareEntering = false;
    private Coroutine flareEntryCoroutine;
public float firsttimeDuration = 1.5f; // 首次渐入持续时间

    public void NotifyTouchedWater()
    {
        if (!hasTouchedWater)
        {
            hasTouchedWater = true;

            // 启动首次渐入协程
            if (flareEntryCoroutine != null) StopCoroutine(flareEntryCoroutine);
            flareEntryCoroutine = StartCoroutine(FlareFirstEntryRoutine());
        }
    }
    
    private IEnumerator FlareFirstEntryRoutine()
{
    isFlareEntering = true;

    float startValue = 0f;
    float valuePercentage = Mathf.Clamp01(currentDisplayValue / maxValue);
    float targetValue = Mathf.Lerp(minFlareIntensity, maxFlareIntensity, valuePercentage);

    float elapsed = 0f;

    while (elapsed < firsttimeDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / firsttimeDuration);
        t = t * t * (3f - 2f * t); // 平滑曲线

        currentFlareIntensity = Mathf.Lerp(startValue, targetValue, t);
        lensFlareSRP.intensity = currentFlareIntensity;

        yield return null;
    }

    isFlareEntering = false;
}


public void SetSuppressDropEffects(bool suppress)
    {
        suppressDropEffects = suppress;
    }




    public enum LightState
    {
        Default,
        Detected,
        Combat
    }

    private void Awake()
    {
        currentDisplayValue = currentValue;
        UpdateLightIntensity();

        currentFlickerFrequency = defaultFlickerFrequency;
        currentFlickerIntensity = defaultFlickerIntensity;
    }

    private void Update()
    {
        if (isDepleting && currentValue > 0)
        {
            currentValue -= depletionRate * depletionMultiplier * Time.deltaTime;

            if (currentValue <= 0)
            {
                currentValue = 0;
                isDepleting = false;
                OnResourceDepleted?.Invoke();
                StartCoroutine(TransitionToDefaultFlicker());
            }
        }

        if (currentValue > currentDisplayValue)
        {
            if (isDecreasing) isDecreasing = false;
            if (!isIncreasing)
            {
                isIncreasing = true;
                increaseTimer = 0f;
                startValue = currentDisplayValue;
                targetValue = currentValue;
            }

            increaseTimer += Time.deltaTime * brightnessIncreaseSpeed;
            float normalizedTime = Mathf.Clamp01(increaseTimer);
            currentDisplayValue = Mathf.Lerp(startValue, targetValue, brightnessIncreaseCurve.Evaluate(normalizedTime));

            if (normalizedTime >= 1.0f)
            {
                isIncreasing = false;
                currentDisplayValue = targetValue;
            }
        }
        else if (currentValue < currentDisplayValue)
        {
            if (isIncreasing) isIncreasing = false;
            if (!isDecreasing)
            {
                isDecreasing = true;
                decreaseTimer = 0f;
                startValue = currentDisplayValue;
                targetValue = currentValue;
            }

            decreaseTimer += Time.deltaTime * brightnessDecreaseSpeed;
            float normalizedTime = Mathf.Clamp01(decreaseTimer);
            currentDisplayValue = Mathf.Lerp(startValue, targetValue, brightnessDecreaseCurve.Evaluate(normalizedTime));

            if (normalizedTime >= 1.0f)
            {
                isDecreasing = false;
                currentDisplayValue = targetValue;
            }
        }
        else
        {
            isIncreasing = false;
            isDecreasing = false;
        }

        UpdateLightIntensity();
    }

  private void UpdateLightIntensity()
{
    if (lensFlareSRP == null) return;

    if (requireWaterContact && !hasTouchedWater)
    {
        lensFlareSRP.intensity = 0f;
        return;
    }

    if (isFlareEntering) return;


        float targetIntensity;

    // ✅ 添加这行：忽略光强更新
    if (suppressDropEffects || isPickedUp || justDropped)
    {
        targetIntensity = 0f;
    }
    else
    {
        float valuePercentage = Mathf.Clamp01(currentDisplayValue / maxValue);
        float baseIntensity = Mathf.Lerp(minFlareIntensity, maxFlareIntensity, valuePercentage);

        flickerTime += Time.deltaTime * currentFlickerFrequency;
        float flickerFactor = Mathf.Sin(flickerTime) * currentFlickerIntensity;
        targetIntensity = baseIntensity * (1.0f + flickerFactor);
    }

    currentFlareIntensity = Mathf.Lerp(currentFlareIntensity, targetIntensity, Time.deltaTime * flareTransitionSpeed);
    lensFlareSRP.intensity = currentFlareIntensity;

    currentDisplayValueDebug = currentDisplayValue;
}
    private IEnumerator TransitionToDefaultFlicker()
    {
        yield return new WaitForSeconds(transitionDelay);
        float startSpeed = currentFlickerFrequency;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);
            currentFlickerFrequency = Mathf.Lerp(startSpeed, defaultFlickerFrequency, t);
            yield return null;
        }

        currentFlickerFrequency = defaultFlickerFrequency;
        SetLightState(LightState.Default);
    }

    public void SetLightState(LightState state)
    {
        currentLightState = state;

        switch (state)
        {
            case LightState.Default:
                currentLightStateDebug = "Default";
                currentFlickerFrequency = defaultFlickerFrequency;
                currentFlickerIntensity = defaultFlickerIntensity;
                break;
            case LightState.Detected:
                currentLightStateDebug = "Detected";
                currentFlickerFrequency = detectedFlickerFrequency;
                currentFlickerIntensity = detectedFlickerIntensity;
                break;
            case LightState.Combat:
                currentLightStateDebug = "Combat";
                currentFlickerFrequency = combatFlickerFrequency;
                currentFlickerIntensity = combatFlickerIntensity;
                break;
        }
    }

    public void StartResourceDepletion(float initialMaxValue)
    {
        if (!isInitialized)
        {
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
        Debug.Log("物品已拾取!");
    }

    public void IsDroppedBy()
    {
        isPickedUp = false;
        Debug.Log("物品已丢弃!");
        StartCoroutine(DelayedDropLight());
    }

    private IEnumerator DelayedDropLight()
{
    justDropped = true;
    isPickedUp = false;  // 立即设置为未拾取状态
    
    // 延迟指定时间
    yield return new WaitForSeconds(dropLightDelay);
    
    // 延迟结束后标记为可以亮起
    justDropped = false;
}


public void AddResource(float amount)
{
    isIncreasing = false;
    SetSuppressDropEffects(true); // ✅ 添加
    StartCoroutine(DelayedAddResource(amount));
}

public IEnumerator DelayedAddResource(float amount)
{
    yield return new WaitForSeconds(resourceAddDelay);

    bool wasEmpty = currentValue <= 0;
    currentValue = Mathf.Min(currentValue + amount, maxValue);

    if (wasEmpty && currentValue > 0)
    {
        SetLightState(LightState.Default);
    }

    // ✅ 添加：恢复亮度控制
    SetSuppressDropEffects(false);
}


    public void ConsumeAllResource()
    {
        currentValue = 0;
        Debug.Log("资源已清零");
    }

    public IEnumerator DelayedPickupResource(float amount)
    {
        yield return new WaitForSeconds(pickupResourceDelay);
        bool wasEmpty = currentValue <= 0;
        currentValue = Mathf.Min(currentValue + amount, maxValue);

        if (wasEmpty && currentValue > 0)
        {
            SetLightState(LightState.Default);
        }

        Debug.Log("拾取模式 - 资源已添加: " + amount);
    }

    public bool IsDecreasing => isDecreasing;
    public bool IsIncreasing => isIncreasing;
}

