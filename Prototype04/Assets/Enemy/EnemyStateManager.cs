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
    private NavMeshAgent navAgent;
    
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

    

    //audio
    private StudioEventEmitter emitter;


    void Start()
    {
        // 初始化 NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        
        // 禁用 NavMeshAgent 的自动旋转，我们手动管理
        navAgent.updateRotation = false;
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

        currentAlertMeter = alertMeter; // 更新当前警戒值

        // 手动更新物体朝向
        //UpdateRotation();

        // 更新 Raycast 方向和距离，改为检测物品
        Vector3 rayDirection = (item.position - transform.position).normalized;
        float distanceToItem = Vector3.Distance(transform.position, item.position);

        // 调用视野检测
        canSeeItem = DetectItem();

        lineRenderer.enabled = canSeeItem;

        // 仅在检测状态发生变化时调用更改方法
        if (canSeeItem != previousCanSeeItem)
        {

            if (canSeeItem) // 检测到物品
            {
                //resourceManager.ChangeUIColor(new Color(1f, 0f, 0f, 1f));
                resourceManager.SetDepletionMultiplier(50f); // 设置资源耗尽速度
                resourceManager.SetDepletionRate(1f); // 设置资源耗尽速率
                AudioManager.instance.PlayOnShotSFX(); //播放被发现的音效
            }
            else
            {
                //resourceManager.ChangeUIColor(new Color(1f, 1f, 1f, 0.5f));
                resourceManager.SetDepletionMultiplier(0f); // 关闭资源耗尽速度
                resourceManager.SetDepletionRate(0f); // 关闭资源耗尽速率
                AudioManager.instance.StopOnShotSFX(); //播放被发现的音效
            }
            // 更新 previousCanSeeItem 状态
            previousCanSeeItem = canSeeItem;
        }

        UpdateAlertMeter();
        UpdateAlertLight();

        // 更新 LineRenderer 的位置和颜色 先关掉射线
        // Vector3 endPoint = transform.position + rayDirection * viewRadius;
        // UpdateLineRenderer(transform.position, endPoint, canSeeItem ? detectedRayColor : defaultRayColor);

        //Debug.Log("Current State: " + currentState);        
    }

    // 手动旋转物体朝向 NavMeshAgent 的前进方向
    /*void UpdateRotation()
    {
        if (navAgent.velocity.sqrMagnitude > 0.1f)  // 当有移动时
        {
            // 计算旋转方向
            Quaternion targetRotation = Quaternion.LookRotation(navAgent.velocity.normalized);

            // 使用 RotateTowards 使物体平滑地旋转到前进方向
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, navAgent.angularSpeed * Time.deltaTime);
        }
    }*/


    // 基于物体当前朝向检测物品
    public bool DetectItem()
    {
        Vector3 dirToItem = item.position - transform.position;
        float distanceToItem = dirToItem.magnitude;
        dirToItem.Normalize();

        float edgeBuffer = 2.5f;  // 视野边缘缓冲区

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

            if (horizontalAngle <= (horizontalViewAngle / 2) - edgeBuffer && 
                Mathf.Abs(verticalAngle) <= (verticalViewAngle / 2) - edgeBuffer)
            {
                RaycastHit[] hits = Physics.RaycastAll(transform.position, dirToItem, distanceToItem);
                foreach (RaycastHit hit in hits)
                {
                    // 如果射线命中的不是 item 本身，说明被其他物体遮挡了
                    if (hit.transform != item)
                    {
                            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Animal"))
                            // 即使命中玩家 会忽视掉 这样即使玩家拿着物品也可以被发现
                            continue;

                        return false;  // 有任意物体挡住了视线
                    }

                    // 检查碰撞物体是否具有 "Cover" 标签
                    /*if (hit.collider.CompareTag("Cover"))
                    {   
                        return false;  // 被标记为“Cover”的物体阻挡了视线
                    } */
                }
                return true; // 未被阻挡，检测到物品
            }
        }
        return false; // 未检测到物品
    }

    void OnDrawGizmos()
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

        if (resourceManager.currentResource <= 0) return;

        if (canSeeItem)
        {
            // 检测到玩家时，警戒值逐渐增加
            alertMeter += Time.deltaTime * alertMeterIncreaseRate;
        }
        else if (canDecreaseAlertMeter)
        {
            // 如果允许，未检测到玩家时警戒值逐渐减少
            alertMeter -= Time.deltaTime * alertMeterDecreaseRate;
        }

        // 限制警戒值在 0 到最大值之间
        alertMeter = Mathf.Clamp(alertMeter, 0, alertMeterMax);

        // 状态切换逻辑
        if (alertMeter >= alertThresholds[2] && !hasSwitchedToCombatState && currentState != CombatState)
        {
            SwitchState(CombatState);
            AudioManager.instance.SetEnemyStateParameter(1);
            hasSwitchedToCombatState = true;
        }
        else if (alertMeter >= alertThresholds[1] && alertMeter < alertThresholds[2] && currentState == PatrolState && !hasSwitchedToAlertState)
        {
            SwitchState(AlertState);
            AudioManager.instance.UpdateStealthMusic();
            emitter.Stop();
            hasSwitchedToAlertState = true;
        }
        else if(playerLost == true && currentState == CombatState && !hasSwitchedToSearchState)
        {
            SwitchState(SearchState);
             AudioManager.instance.SetEnemyStateParameter(2);
            hasSwitchedToSearchState = true;
        }
        else if (alertMeter < alertThresholds[0] && currentState == SearchState && !hasSwitchedToPatrolState)
        {
            SwitchState(PatrolState);
            AudioManager.instance.StopStealthMusic();
            emitter.Play();
            hasSwitchedToPatrolState = true;
        }
        else if (currentState == AlertState  && !hasSwitchedToSearchState)
        {
            SwitchState(SearchState);
            AudioManager.instance.SetEnemyStateParameter(2);
            hasSwitchedToSearchState = true;
        }
    }

    void UpdateAlertLight()
    {
        if (alertSpotLight != null)  // 确保 SpotLight 存在
        {
            // 根据警戒值确定目标颜色
            if (alertMeter <= 25)
            {
                targetColor = lowAlertColor;
            }
            else if (alertMeter > 25 && alertMeter <= 50)
            {
                targetColor = mediumAlertColor;
            }
            else if (alertMeter > 50 && alertMeter < 100)
            {
                targetColor = highAlertColor;
            }
            else if (alertMeter >= 100)
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

}
