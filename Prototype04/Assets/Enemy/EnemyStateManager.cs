using Audio;
using UnityEngine;
using UnityEngine.AI;
using FMOD.Studio;
using FMODUnity;
using FMODUnityResonance;


[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(StudioEventEmitter))]
public class EnemyStateManager : MonoBehaviour
{
    
    [Header("Debug")]
    [SerializeField, Tooltip("当前敌人的状态，仅用于调试")]
    private string currentStateName;
    [SerializeField, Tooltip("当前警戒值，仅用于调试")]
    private float currentAlertMeter;
    EnemyBaseState currentState;
    public EnemyPatrolState PatrolState = new EnemyPatrolState();
    public EnemyAlertState AlertState = new EnemyAlertState();
    public EnemyCombatState CombatState = new EnemyCombatState();
    public EnemySearchState SearchState = new EnemySearchState();
    public EnemyOffState OffState = new EnemyOffState();

    [Header("Detection Parameters")] // 
    public Transform item;  // 改为检测物品
    public float viewRadius = 10f;  // 视野范围
    [Range(0, 360)] public float horizontalViewAngle = 90f;  // 水平视野角度
    [Range(0, 180)] public float verticalViewAngle = 60f;  // 垂直视野角度    
    private LineRenderer lineRenderer;  // 显示射线
    [HideInInspector] public bool canSeeItem = false;  // 现在检测物品
    [HideInInspector] public Color defaultRayColor = Color.blue;  // 默认射线颜色
    [HideInInspector] public Color detectedRayColor = Color.red;  // 检测到物品的颜色

    [Header("Patrol System")] // 
    // Waypoints for patrolling
    public Transform[] waypoints;  // 巡逻点数组
    public float waypointReachThreshold = 0.5f;  // 到达巡逻点的距离阈值
    public float waitTimeAtWaypoint = 2f;  // 每个巡逻点的停留时间

    [Header("Patrol Rotation System")]
    public float[] rotationSpeeds; // 每个旋转目标的旋转速度，与 waypoints 对应
    public float defaultRotationSpeed = 10f; // 默认旋转速度

    // Speed
    public float patrolSpeed = 2f;  // 巡逻速度
    
    //alert system
    // 警戒参数
    [Header("Alert Bar")] // 
    public float alertMeter = 0;  // 当前警戒值
    
    public float alertMeterMax = 100;  // 警戒条最大值
    public float alertMeterIncreaseRate = 5f;  // 检测到玩家时警戒条增加速度
    public float alertMeterDecreaseRate = 3f;  // 未检测到玩家时警戒条减少速度

    // 定义多个警戒值阈值
    public float[] alertThresholds = { 25f, 50f, 100f };

    [HideInInspector] public bool canDecreaseAlertMeter = true;

    [Header("Rotation System")] // 
    public bool shouldRotate = false; 

    //CCTV Enemy 
    public Vector3[] rotationPoints;  // 定义旋转目标角度（欧拉角）
    public float rotationSpeed = 10f;  // 旋转速度
    public float waitTimeAtPoint = 2f;  // 每个角度停留时间
    [HideInInspector] public int currentRotationIndex = 0;  // 当前旋转角度的索引
    [HideInInspector] public Quaternion targetRotation;  // 当前目标旋转
    [HideInInspector] public float waitTimer = 0f;  // 停留计时器
    private bool previousCanSeeItem = false; // 用于跟踪上一次的 canSeeItem 状态

    [HideInInspector] public Light alertSpotLight;  // 用于引用 SpotLight

    
    [Header("Search State Rotation System")]
    public Transform[] searchTargets;         // 搜索目标点数组
    public float[] searchRotationSpeeds;      // 每个目标的旋转速度数组
    public float defaultSearchRotationSpeed = 5f; // 默认旋转速度
    public float searchWaitTimeAtPoint = 1.5f; // 在目标点的停留时间
    [HideInInspector] public int searchTargetIndex = 0; // 当前目标点的索引
    [HideInInspector] public Quaternion searchTargetRotation; // 当前目标的旋转
    [HideInInspector] public float searchWaitTimer = 0f; // 等待计时器

    [Header("Light Colors")] // 在 Inspector 窗口中自定义颜色
    public Color lowAlertColor = Color.white;
    public Color mediumAlertColor = new Color(1f, 0.65f, 0f);   // 默认橙色
    public Color highAlertColor = new Color(1f, 0.4f, 0f);       // 默认深橙色
    public Color maxAlertColor =  new Color(1f, 0f, 0f);         // 默认红色

    private Color targetColor;     // 目标颜色
    public float colorLerpSpeed = 2f;  // 控制 Lerp 速度

    [System.Serializable]
    public class FlickerSettings
    {
        public float frequency = 2f;         // 闪烁频率（Hz）
        public float intensityMin = 6000000f;    // 最低强度
        [HideInInspector] public float intensityMax = 1f;  // 初始默认值
    }

    public FlickerSettings patrolFlicker;
    public FlickerSettings alertFlicker;
    public FlickerSettings combatFlicker;
    public FlickerSettings searchFlicker;
    public FlickerSettings offFlicker;


    private ResourceManager resourceManager;

    [Header("Share Alert Among Enemies")] // 
    public float alertRadius = 10f;     // 警戒范围
    public LayerMask enemyLayer;        // 用于筛选敌人层
    private bool hasBroadcasted = false;
    public bool playerLost = false; // 用于标记 CombatState 是否丢失玩家
    [HideInInspector] public bool hasSwitchedToCombatState = false; // 防止重复切换到 CombatState
    private bool hasSwitchedToSearchState = false; // 确保切换只执行一次
    private bool hasSwitchedToPatrolState = false; // 确保切换只执行一次
    private bool hasSwitchedToAlertState = false; // 确保切换只执行一次

    [Header("Resource Depletion Settings")]
    private float defaultDepletionMultiplier = 1f;  // 默认状态下的消耗倍率
    private float combatDepletionMultiplier = 5f;  // 战斗状态下的消耗倍率



[Header("Movement Jitter Settings")]
// 全局抖动设置
public bool enableJitter = true;                 // 是否启用抖动效果

// 巡逻状态抖动设置
[Header("Patrol State Jitter")]
public float patrolJitterRange = 0.2f;           // 巡逻状态抖动范围
public float patrolJitterSmoothSpeed = 3.0f;     // 巡逻状态抖动平滑速度
public float patrolJitterUpdateInterval = 0.2f;  // 巡逻状态抖动更新间隔

// 警戒状态抖动设置
[Header("Alert State Jitter")]
public float alertJitterRange = 0.5f;            // 警戒状态抖动范围
public float alertJitterSmoothSpeed = 4.0f;      // 警戒状态抖动平滑速度
public float alertJitterUpdateInterval = 0.15f;  // 警戒状态抖动更新间隔

// 战斗状态抖动设置
[Header("Combat State Jitter")]
public float combatJitterRange = 0.7f;           // 战斗状态抖动范围  
public float combatJitterSmoothSpeed = 5.0f;     // 战斗状态抖动平滑速度
public float combatJitterUpdateInterval = 0.1f;  // 战斗状态抖动更新间隔

// 搜索状态抖动设置
[Header("Search State Jitter")]
public float searchJitterRange = 0.4f;           // 搜索状态抖动范围
public float searchJitterSmoothSpeed = 3.5f;     // 搜索状态抖动平滑速度
public float searchJitterUpdateInterval = 0.15f; // 搜索状态抖动更新间隔
[Header("Off State Jitter")]
public float offJitterRange = 0.0f;             // 关闭状态抖动范围
public float offJitterSmoothSpeed = 0.0f;       // 关闭状态抖动平滑速度
public float offJitterUpdateInterval = 0.0f;   // 关闭状态抖动更新间隔

// 当前抖动状态参数
[HideInInspector] public Vector3 currentJitter = Vector3.zero;  // 当前抖动值
[HideInInspector] public Vector3 targetJitter = Vector3.zero;   // 目标抖动值
private float jitterTimer = 0f;                                 // 抖动更新计时器

// 当前使用的抖动参数
private float currentJitterRange = 0.2f;
private float currentJitterSmoothSpeed = 3.0f;
private float currentJitterUpdateInterval = 0.2f;

    //audio
    private StudioEventEmitter emitter;

[Header("敌人关闭灯光渐出")]
public float fadeOutDelay = 1.5f;
public float fadeOutDuration = 3.5f;


    void Start()
    {
        Debug.Log("Start From Patrol State");
        currentState = PatrolState;
        currentState.EnterState(this);

        // 初始化 LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));  // 确保 LineRenderer 可以改变颜色

        // 初始化警戒条
        if (alertMeter >= alertMeterMax)
        {
            alertMeter = alertMeterMax;
            canDecreaseAlertMeter = false;
        }

        if (rotationPoints.Length > 0)
        {
            targetRotation = Quaternion.Euler(rotationPoints[currentRotationIndex]);
        }

        alertSpotLight = transform.Find("Spot Light").GetComponent<Light>();

        

        // 确保找到 SpotLight
        if (alertSpotLight == null)
        {
            
            float defaultIntensity = alertSpotLight.intensity;

            patrolFlicker.intensityMax = defaultIntensity;
            alertFlicker.intensityMax = defaultIntensity;
            combatFlicker.intensityMax = defaultIntensity;
            searchFlicker.intensityMax = defaultIntensity;

            Debug.LogError("SpotLight not found! Please ensure it is a child of this GameObject.");
        }

        

        resourceManager = FindAnyObjectByType<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not found in the scene!");
        }
        else
        {
            resourceManager.OnResourceDepleted += HandleResourceDepleted; // 订阅资源耗尽事件
        }

        AudioManager.instance.InitializeStealthMusic(FMODEvents.instance.stealthMusic); // 初始化音乐
        AudioManager.instance.InitializeOnShotSFX(FMODEvents.instance.onShotSFX); // 初始化音效
        emitter = AudioManager.instance.InitializeEventEmitter(FMODEvents.instance.patrolbassSFX, this.gameObject);
        emitter.Play();
    }

    void Update()
    {
        if (currentState != null)
        {
            currentStateName = currentState.GetType().Name; // 更新当前状态名称
        }
        currentState.UpdateState(this);

        if (currentState == OffState)
        {

            ApplyLightFlicker();
            UpdateJitterEffect(); // 如果你希望 Off 状态下仍保留微弱晃动感
            return; // 直接返回，跳过所有检测逻辑
        }

        currentAlertMeter = alertMeter; // 更新当前警戒值

        // 手动更新物体朝向
        //UpdateRotation();

        // 更新 Raycast 方向和距离，改为检测物品
        Vector3 rayDirection = (item.position - transform.position).normalized;
        float distanceToItem = Vector3.Distance(transform.position, item.position);

        // 调用视野检测
        canSeeItem = DetectItem();

        lineRenderer.enabled = canSeeItem;

    if (canSeeItem != previousCanSeeItem)
    {
        if (canSeeItem) // 检测到物品
        {
            // 根据当前状态设置适当的消耗倍率
            if (currentState == CombatState)
            {
                resourceManager.SetDepletionMultiplier(combatDepletionMultiplier);
                // 设置灯光为战斗状态
                resourceManager.SetLightState(ResourceManager.LightState.Combat);
            }
            else
            {
                resourceManager.SetDepletionMultiplier(defaultDepletionMultiplier);
                // 设置灯光为检测状态
                resourceManager.SetLightState(ResourceManager.LightState.Detected);
            }
            
            resourceManager.SetDepletionRate(2f); // 设置基础消耗速率
            resourceManager.StartResourceDepletion(resourceManager.currentValue);
            
            //AudioManager.instance.PlayOnShotSFX();
        }
        else
        {
            resourceManager.SetDepletionMultiplier(0f); // 关闭资源消耗
            resourceManager.SetDepletionRate(0f);
            resourceManager.StopResourceDepletion(); // 停止资源消耗
            
            // 恢复默认灯光状态
            resourceManager.SetLightState(ResourceManager.LightState.Default);
            
            //AudioManager.instance.StopOnShotSFX();
        }
        
        previousCanSeeItem = canSeeItem;
    }

            UpdateAlertMeter();
            UpdateAlertLight();
            ApplyLightFlicker();
                UpdateJitterEffect();

        }


    // 基于物体当前朝向检测物品
    public bool DetectItem()
{
    Vector3 dirToItem = item.position - transform.position;
    float distanceToItem = dirToItem.magnitude;
    dirToItem.Normalize();

    float edgeBuffer = 2.5f;  // 视野边缘缓冲区

    //Debug.Log($"[DetectItem] ▶ Checking detection...");
    //Debug.Log($"[DetectItem] Distance to item: {distanceToItem:F2}, View Radius: {viewRadius}");

    if (distanceToItem <= viewRadius)
    {
        Vector3 horizontalDir = Vector3.ProjectOnPlane(dirToItem, transform.up).normalized;
        float dotHorizontal = Vector3.Dot(transform.forward, horizontalDir);
        float horizontalAngle = Mathf.Acos(dotHorizontal) * Mathf.Rad2Deg;

        float dotVertical = Vector3.Dot(transform.forward, dirToItem);
        float totalAngle = Mathf.Acos(dotVertical) * Mathf.Rad2Deg;
        float verticalAngle = totalAngle - horizontalAngle;

        if (Vector3.Dot(transform.up, dirToItem) < 0)
        {
            verticalAngle = -verticalAngle;
        }

        //Debug.Log($"[DetectItem] Horizontal Angle: {horizontalAngle:F1}°, Limit: {(horizontalViewAngle / 2f) - edgeBuffer:F1}°");
        //Debug.Log($"[DetectItem] Vertical Angle: {verticalAngle:F1}°, Limit: {(verticalViewAngle / 2f) - edgeBuffer:F1}°");

        if (horizontalAngle <= (horizontalViewAngle / 2) - edgeBuffer &&
            Mathf.Abs(verticalAngle) <= (verticalViewAngle / 2) - edgeBuffer)
        {
           // Debug.Log("[DetectItem] ✅ Angle check PASSED. Proceeding to raycast...");

            RaycastHit[] hits = Physics.RaycastAll(transform.position, dirToItem, distanceToItem);
            //Debug.Log($"[DetectItem] Raycast hit count: {hits.Length}");

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform != item)
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Animal"))
                    {
                        //Debug.Log($"[DetectItem] ⚠️ Hit Animal: {hit.collider.name}, ignoring...");
                        continue;
                    }

                    //Debug.Log($"[DetectItem] ❌ Blocked by: {hit.collider.name}, layer: {hit.collider.gameObject.layer}");
                    return false;
                }
            }

            //Debug.Log("[DetectItem] ✅ Detection SUCCESS: No obstacles.");
            return true;
        }
        else
        {
            //Debug.Log("[DetectItem] ❌ Angle check FAILED");
        }
    }
    else
    {
        //Debug.Log("[DetectItem] ❌ Distance check FAILED");
    }

    return false;
}    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 绘制水平视野
        Vector3 leftDir = Quaternion.AngleAxis(-horizontalViewAngle / 2, transform.up) * transform.forward;
        Vector3 rightDir = Quaternion.AngleAxis(horizontalViewAngle / 2, transform.up) * transform.forward;
        
        // 绘制垂直视野
        Vector3 upDir = Quaternion.AngleAxis(verticalViewAngle / 2, transform.right) * transform.forward;
        Vector3 downDir = Quaternion.AngleAxis(-verticalViewAngle / 2, transform.right) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftDir * viewRadius);
        Gizmos.DrawRay(transform.position, rightDir * viewRadius);
        Gizmos.DrawRay(transform.position, upDir * viewRadius);
        Gizmos.DrawRay(transform.position, downDir * viewRadius);

        // draw view cone
        Gizmos.color = new Color(0, 1, 1, 0.2f); 
        DrawViewCone(leftDir, rightDir, upDir, downDir);

        if (DetectItem())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, item.position);
        }
    }

    void DrawViewCone(Vector3 leftDir, Vector3 rightDir, Vector3 upDir, Vector3 downDir)
    {
        Vector3[] vertices = new Vector3[4];
        vertices[0] = transform.position + upDir * viewRadius;
        vertices[1] = transform.position + downDir * viewRadius;
        vertices[2] = transform.position + leftDir * viewRadius;
        vertices[3] = transform.position + rightDir * viewRadius;

        Gizmos.DrawLine(transform.position, vertices[0]);
        Gizmos.DrawLine(transform.position, vertices[1]);
        Gizmos.DrawLine(transform.position, vertices[2]);
        Gizmos.DrawLine(transform.position, vertices[3]);

        Gizmos.DrawLine(vertices[0], vertices[2]);
        Gizmos.DrawLine(vertices[0], vertices[3]);
        Gizmos.DrawLine(vertices[1], vertices[2]);
        Gizmos.DrawLine(vertices[1], vertices[3]);
    }

void UpdateAlertMeter()
{
    if (resourceManager.currentValue <= 0) return;

    if (canSeeItem)
    {
        alertMeter += Time.deltaTime * alertMeterIncreaseRate;
    }
    else if (canDecreaseAlertMeter)
    {
        alertMeter -= Time.deltaTime * alertMeterDecreaseRate;
    }

    alertMeter = Mathf.Clamp(alertMeter, 0, alertMeterMax);

    // Combat 状态（最高优先级）
    if (alertMeter >= alertThresholds[2] && !hasSwitchedToCombatState && currentState != CombatState)
    {
        SwitchState(CombatState);
        AudioManager.instance.SetEnemyStateParameter(1);
        hasSwitchedToCombatState = true;
    }

    // 从 Patrol 进入 Alert
    else if (alertMeter >= alertThresholds[1] && alertMeter < alertThresholds[2] && currentState == PatrolState && !hasSwitchedToAlertState)
    {
        SwitchState(AlertState);
        AudioManager.instance.UpdateStealthMusic();
        emitter.Stop();
        hasSwitchedToAlertState = true;
    }

    // Combat 丢失目标 → Search
    else if (playerLost == true && currentState == CombatState && !hasSwitchedToSearchState)
    {
        SwitchState(SearchState);
        AudioManager.instance.SetEnemyStateParameter(2);
        hasSwitchedToSearchState = true;
    }

    // Alert 状态警戒值归零 → 回 Patrol
    else if (alertMeter < alertThresholds[0] && currentState == AlertState && !hasSwitchedToPatrolState)
    {
        SwitchState(PatrolState);
        AudioManager.instance.StopStealthMusic();
        emitter.Play();
        hasSwitchedToPatrolState = true;
    }

    // Search 状态警戒值归零 → 回 Patrol
    else if (alertMeter < alertThresholds[0] && currentState == SearchState && !hasSwitchedToPatrolState)
    {
        SwitchState(PatrolState);
        AudioManager.instance.StopStealthMusic();
        emitter.Play();
        hasSwitchedToPatrolState = true;
    }
}
    void UpdateAlertLight()
    {
        if (alertSpotLight != null)  // 确保 SpotLight 存在
        {
            // 根据警戒值确定目标颜色
            if (alertMeter <= alertThresholds[0])
            {
                targetColor = lowAlertColor;
            }
            else if (alertMeter > alertThresholds[0] && alertMeter <= alertThresholds[1])
            {
                targetColor = mediumAlertColor;
            }
            else if (alertMeter > alertThresholds[1] && alertMeter < alertThresholds[2])
            {
                targetColor = highAlertColor;
            }
            else if (alertMeter >= alertThresholds[2])
            {
                targetColor = maxAlertColor;
                canDecreaseAlertMeter = false;  // 达到最大值后停止降低
            }

            // 使用 Lerp 平滑过渡到目标颜色
            alertSpotLight.color = Color.Lerp(alertSpotLight.color, targetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    // 更新 LineRenderer 的位置和颜色
    private void UpdateLineRenderer(Vector3 startPosition, Vector3 endPosition, Color color)
    {
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
    }

    public void SwitchState(EnemyBaseState newState, bool suppressBroadcast = false) // 切换状态
    {
        if (currentState != null)
        {
            currentState.ExitState(this); // 退出当前状态
        }

        currentState = newState;
        currentState.EnterState(this); // 进入新状态

        currentStateName = currentState.GetType().Name; // 更新状态名称 在Inspector中
        //SwitchMusicByState(newState); // 切换音乐

        if (resourceManager != null)
        {
            // 如果是战斗状态，使用高消耗倍率
            if (newState == CombatState)
            {
                resourceManager.SetDepletionMultiplier(combatDepletionMultiplier);
                // 设置为战斗状态的闪烁效果
                resourceManager.SetLightState(ResourceManager.LightState.Combat);
            }
            // 否则使用默认消耗倍率
            else
            {
                resourceManager.SetDepletionMultiplier(defaultDepletionMultiplier);
                // 如果能看到物品，设置为检测状态的闪烁效果，否则设置为默认状态
                if (canSeeItem)
                {
                    resourceManager.SetLightState(ResourceManager.LightState.Detected);
                }
                else
                {
                    resourceManager.SetLightState(ResourceManager.LightState.Default);
                }
            }
            
            // 如果能看到物品，确保启动资源消耗
            if (canSeeItem)
            {
                resourceManager.StartResourceDepletion(resourceManager.currentValue);
            }
    }

        hasSwitchedToCombatState = false; // 重置状态切换标志
        hasSwitchedToAlertState = false; // 重置状态切换标志
        hasSwitchedToSearchState = false; // 重置状态切换标志
        hasSwitchedToPatrolState = false; // 重置状态切换标志

      if (!suppressBroadcast)
        hasBroadcasted = false;
    }

    public void BroadcastCombat() // 在一定范围内和其他敌人共享战斗警报
    {
        if (hasBroadcasted) return;
        hasBroadcasted = true; // 防止重复调用

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, alertRadius, enemyLayer);

        if (hitColliders.Length == 0)
        {
            Debug.Log("No enemies detected in alert radius.");
            return;
        }

        foreach (Collider collider in hitColliders)
        {   
            EnemyStateManager enemy = collider.GetComponent<EnemyStateManager>();
            enemy.alertMeter = alertMeter; // 传递警戒值

            if (enemy != null && enemy != this) // 确保不是自己
            {
                if (enemy.currentState != enemy.CombatState) // 如果敌人当前不是 Combat 状态
                {
                    enemy.alertMeter = alertMeter; // 同步警戒值
                    enemy.SwitchState(enemy.CombatState, true); // 切换到 Combat 状态
                }
            }
        }
    }

[HideInInspector] public float flickerFadeMultiplier = 1f; // 默认全亮

private void ApplyLightFlicker()
{
    {
    if (alertSpotLight == null) return;

    FlickerSettings settings = patrolFlicker;

    if (currentState == AlertState)
        settings = alertFlicker;
    else if (currentState == CombatState)
        settings = combatFlicker;
    else if (currentState == SearchState)
        settings = searchFlicker;
    else if (currentState == OffState)
        settings = offFlicker;

    float flicker = Mathf.Lerp(settings.intensityMin, settings.intensityMax,
        Mathf.PerlinNoise(Time.time * settings.frequency, 0f));

    // ⚠️ 加入 fadeMultiplier 缩放控制
    alertSpotLight.intensity = flicker * flickerFadeMultiplier;
}
}


private void UpdateJitterEffect()
{
    if (!enableJitter) return;
    
    // 根据当前状态设置抖动参数
    UpdateJitterParameters();
    
    // 定时更新目标抖动值
    jitterTimer += Time.deltaTime;
    if (jitterTimer >= currentJitterUpdateInterval)
    {
        targetJitter = new Vector3(
            Random.Range(-currentJitterRange, currentJitterRange),
            Random.Range(-currentJitterRange, currentJitterRange),
            Random.Range(-currentJitterRange, currentJitterRange)
        ).normalized * currentJitterRange;
        jitterTimer = 0f;
    }
    
    // 平滑更新当前抖动值
    currentJitter = Vector3.Lerp(currentJitter, targetJitter, Time.deltaTime * currentJitterSmoothSpeed);
}
private void UpdateJitterParameters()
{
    // 根据当前状态设置相应的抖动参数
    if (currentState == PatrolState)
    {
        currentJitterRange = patrolJitterRange;
        currentJitterSmoothSpeed = patrolJitterSmoothSpeed;
        currentJitterUpdateInterval = patrolJitterUpdateInterval;
    }
    else if (currentState == AlertState)
    {
        currentJitterRange = alertJitterRange;
        currentJitterSmoothSpeed = alertJitterSmoothSpeed;
        currentJitterUpdateInterval = alertJitterUpdateInterval;
    }
    else if (currentState == CombatState)
    {
        currentJitterRange = combatJitterRange;
        currentJitterSmoothSpeed = combatJitterSmoothSpeed;
        currentJitterUpdateInterval = combatJitterUpdateInterval;
    }
    else if (currentState == SearchState)
    {
        currentJitterRange = searchJitterRange;
        currentJitterSmoothSpeed = searchJitterSmoothSpeed;
        currentJitterUpdateInterval = searchJitterUpdateInterval;
    }
    else if (currentState == OffState)
    {
        currentJitterRange = 0f; // 关闭抖动
        currentJitterSmoothSpeed = 0f;
        currentJitterUpdateInterval = 0f;
    }
}

public Quaternion GetJitteredRotation(Quaternion baseRotation)
{
    if (!enableJitter || currentJitter == Vector3.zero)
        return baseRotation;
    
    // 从基础旋转获取前方向
    Vector3 forward = baseRotation * Vector3.forward;
    
    // 应用抖动
    Vector3 jitteredForward = forward + currentJitter;
    
    // 返回新的旋转
    return Quaternion.LookRotation(jitteredForward);
}


    
// 敌人警报的传递范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }

    void OnDestroy()
    {
        currentState = null; // 释放状态引用
    }

    private void HandleResourceDepleted() // 当资源耗尽时调用
    {
        if (currentState != OffState)
        {
            SwitchState(OffState);
        }
    }

        public void TurnOffMusic()
    {

        AudioManager.instance.StopStealthMusic(true);
    }



}
