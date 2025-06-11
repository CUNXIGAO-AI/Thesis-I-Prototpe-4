using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.SceneManagement; // 引入场景管理命名空间

public class NarrativeManager : MonoBehaviour
{
    // Start is called before the first frame updateusing System.Collections;
    // Start is called before the first frame update
    public static NarrativeManager Instance { get; private set; }
    public GameObject fakeSkybox;
    public Animator npcAnimator;  // 在 Inspector 中拖入 NPC 的 Animator 组件
    public string deadTriggerName = "Dead";
    public string liveTriggerName = "Live";

    // ✅ 新增：存储所有同步的Animator实例
    private List<Animator> synchronizedAnimators = new List<Animator>();
    
      // 交互状态追踪
    private bool firstInteractionGift = false;  // 第一次交互是否给了礼物
    private bool secondInteractionGift = false; // 第二次交互是否给了礼物
    private bool firstInteractionCompleted = false; // 第一次交互是否已完成
    private bool secondInteractionCompleted = false; // 第二次交互是否已完成
    
    // Skybox状态
    private bool skyboxActive = false;

     [Tooltip("第一次交互给予礼物的快捷键")]
    public KeyCode giveFirstGiftKey = KeyCode.F1;
    
    [Tooltip("第一次交互不给礼物的快捷键")]
    public KeyCode denyFirstGiftKey = KeyCode.F2;
    
    [Tooltip("第二次交互给予礼物的快捷键")]
    public KeyCode giveSecondGiftKey = KeyCode.F3;
    
    [Tooltip("第二次交互不给礼物的快捷键")]
    public KeyCode denySecondGiftKey = KeyCode.F4;
    [Header("调试信息")]
    [SerializeField] private bool showDebugMessages = true;

        [Header("重载场景后引用恢复设置")]
    [SerializeField] private string skyboxTag = "fakeSkybox";
    [SerializeField] private string npcAnimatorTag = "girlAnimator";
    [Header("结局音效播放设置")]
public float endingSFXDelay = 2f;
    
    // NPC状态
    public enum NPCState
    {
        Normal,  // 初始状态
        Live,    // 存活
        Dead     // 死亡
    }
    
    private NPCState currentNPCState = NPCState.Normal;
    
    // 结局类型枚举
    public enum EndingType
    {
        GoodEnding,    // 好结局 (NPC存活)
        BadEnding,     // 坏结局 (NPC死亡，第一次给了礼物)
        WorstEnding    // 最坏结局 (NPC死亡，第一次没给礼物)
    }
    
    // 结局触发状态
    private bool endingTriggered = false;
    
    // 事件委托声明
    public delegate void EndingTriggeredDelegate(EndingType endingType);
    public event EndingTriggeredDelegate OnEndingTriggered;
    
    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
    
    private void Start()
    {
        // 在游戏一开始就保存引用
        if (fakeSkybox == null)
        {
            fakeSkybox = GameObject.FindGameObjectWithTag("FakeSkybox");
        }
        
        // 根据初始游戏状态决定是否显示
        // 如果游戏刚开始不需要显示 Skybox，就在获取引用后立即关闭它
        if (fakeSkybox != null && !skyboxActive)
        {
            fakeSkybox.SetActive(false);
        }
        
        // ✅ 初始时查找所有相关的Animator
        FindAllSynchronizedAnimators();
    }

    private void Update()
    {
        // 第一次交互快捷键
        if (Input.GetKeyDown(giveFirstGiftKey) && !firstInteractionCompleted)
        {
            HandleFirstInteraction(true); // 给礼物
        }
        
        if (Input.GetKeyDown(denyFirstGiftKey) && !firstInteractionCompleted)
        {
            HandleFirstInteraction(false); // 不给礼物
        }
        
        // 第二次交互快捷键
        if (Input.GetKeyDown(giveSecondGiftKey) && firstInteractionCompleted && !secondInteractionCompleted)
        {
            HandleSecondInteraction(true); // 给礼物
        }
        
        if (Input.GetKeyDown(denySecondGiftKey) && firstInteractionCompleted && !secondInteractionCompleted)
        {
            HandleSecondInteraction(false); // 不给礼物
        }
                
    }

    // ✅ 新增：查找场景中所有使用相同Animator Controller的实例
    private void FindAllSynchronizedAnimators()
    {
        synchronizedAnimators.Clear();
        
        // 方法1：通过标签查找
        GameObject[] npcObjs = GameObject.FindGameObjectsWithTag(npcAnimatorTag);
        foreach (GameObject obj in npcObjs)
        {
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                synchronizedAnimators.Add(animator);
                if (showDebugMessages)
                    Debug.Log($"[NarrativeManager] 通过标签找到Animator: {obj.name}");
            }
        }
        
        // 方法2：如果有主要的npcAnimator，找所有使用相同Controller的实例
        if (npcAnimator != null && npcAnimator.runtimeAnimatorController != null)
        {
            RuntimeAnimatorController targetController = npcAnimator.runtimeAnimatorController;
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            
            foreach (Animator animator in allAnimators)
            {
                if (animator.runtimeAnimatorController == targetController && !synchronizedAnimators.Contains(animator))
                {
                    synchronizedAnimators.Add(animator);
                    if (showDebugMessages)
                        Debug.Log($"[NarrativeManager] 通过Controller匹配找到Animator: {animator.gameObject.name}");
                }
            }
        }
        
        // 方法3：如果还没找到，查找所有包含目标触发器的Animator
        if (synchronizedAnimators.Count == 0)
        {
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            foreach (Animator animator in allAnimators)
            {
                if (HasRequiredAnimatorParameters(animator))
                {
                    synchronizedAnimators.Add(animator);
                    if (showDebugMessages)
                        Debug.Log($"[NarrativeManager] 通过参数匹配找到Animator: {animator.gameObject.name}");
                }
            }
        }
        
        if (showDebugMessages)
            Debug.Log($"[NarrativeManager] 总共找到 {synchronizedAnimators.Count} 个需要同步的Animator");
    }
    
    // ✅ 检查Animator是否包含所需的参数
    private bool HasRequiredAnimatorParameters(Animator animator)
    {
        if (animator.runtimeAnimatorController == null) return false;
        
        // 检查是否有所需的触发器
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == deadTriggerName || 
                parameter.name == liveTriggerName )
            {
                return true;
            }
        }
        return false;
    }

    // 处理第一次交互的选择
    public void HandleFirstInteraction(bool gaveGift)
    {
        if (firstInteractionCompleted) 
        {
            Debug.Log("第一次交互已完成，忽略重复处理");
            return;
        }
        
        firstInteractionGift = gaveGift;
        firstInteractionCompleted = true;
        
        // 如果给了礼物，打开 Skybox
        if (gaveGift)
        {
            // 此时引用已存在，不会丢失
            if (fakeSkybox != null)
            {
                fakeSkybox.SetActive(true);
            }
            skyboxActive = true;
            Debug.Log("第一次交互给予礼物: 打开Skybox");
        }
        else
        {
            // 如果未给礼物，确保 Skybox 关闭
            if (fakeSkybox != null)
            {
                fakeSkybox.SetActive(false);
            }
            skyboxActive = false;
            Debug.Log("第一次交互未给礼物: Skybox保持关闭");
        }
        
        Debug.Log($"第一次交互完成: 给予礼物={gaveGift}, Skybox状态={skyboxActive}");
    }

    // 处理第二次交互的选择
    public void HandleSecondInteraction(bool gaveGift)
    {
        if (!firstInteractionCompleted)
        {
            Debug.Log("错误: 尝试进行第二次交互，但第一次交互尚未完成");
            return;
        }
        
        if (secondInteractionCompleted)
        {
            Debug.Log("第二次交互已完成，忽略重复处理");
            return;
        }
        
        secondInteractionGift = gaveGift;
        secondInteractionCompleted = true;
        
        Debug.Log($"第二次交互完成: 给予礼物={gaveGift}");
        
        // 根据两次交互结果确定NPC状态和结局
        DetermineOutcome();
    }
    
    // 确定游戏结果
    private void DetermineOutcome()
    {
        EndingType currentEnding;
        
        if (firstInteractionGift)
        {
            if (secondInteractionGift)
            {
                // 第一次给礼物，第二次给礼物 => NPC存活，好结局
                currentNPCState = NPCState.Live;
                currentEnding = EndingType.GoodEnding;
                Debug.Log("结局确定: 好结局 (Skybox开启, NPC存活)");
            }
            else
            {
                // 第一次给礼物，第二次不给 => NPC死亡，坏结局
                currentNPCState = NPCState.Dead;
                currentEnding = EndingType.BadEnding;
                Debug.Log("结局确定: 坏结局 (Skybox开启, NPC死亡)");
            }
        }
        else
        {
            // 第一次不给礼物，第二次无论如何 => NPC死亡，最坏结局
            currentNPCState = NPCState.Dead;
            currentEnding = EndingType.WorstEnding;
            Debug.Log($"结局确定: 最坏结局 (Skybox关闭, NPC死亡, 第二次给予礼物={secondInteractionGift})");
        }
        
        // 使用新的方法更新所有Animator状态
        UpdateAllNPCAnimators();
        
        // 触发结局
        TriggerEnding(currentEnding);
    }
    
    // 触发结局
    public void TriggerEnding(EndingType endingType)
    {
        if (endingTriggered)
        {
            Debug.Log("结局已经触发，忽略重复触发");
            return;
        }
        
        endingTriggered = true;
        
        if (showDebugMessages)
        {
            Debug.Log($"[NarrativeManager] 开始触发结局: {endingType}");
            Debug.Log($"[NarrativeManager] 将同步 {synchronizedAnimators.Count} 个Animator");
        }
        
        // ✅ 修改：对所有同步的Animator设置状态
        SetTriggerForAllAnimators(currentNPCState);
        
        Debug.Log($"触发结局: {endingType}, NPC状态: {currentNPCState}, Skybox状态: {skyboxActive}");
        
        if (AudioManager.instance != null)
        {
            StartCoroutine(PlayEndingSFXWithDelay(endingType));
        }
    }

    private IEnumerator PlayEndingSFXWithDelay(EndingType endingType)
{
    yield return new WaitForSeconds(endingSFXDelay);

    switch (endingType)
    {
        case EndingType.GoodEnding:
            FMODUnity.RuntimeManager.PlayOneShot(FMODEvents.instance.goodEndingSFX);
            break;
        case EndingType.BadEnding:
            FMODUnity.RuntimeManager.PlayOneShot(FMODEvents.instance.badEndingSFX);
            break;
        case EndingType.WorstEnding:
            FMODUnity.RuntimeManager.PlayOneShot(FMODEvents.instance.badEndingSFX);
            break;
    }

    if (showDebugMessages)
        Debug.Log($"[NarrativeManager] 已播放 {endingType} 的音效");
}

    // ✅ 新增：对所有Animator设置触发器的方法
    private void SetTriggerForAllAnimators(NPCState state)
    {
        string triggerName = "";
    switch (state)
    {
        case NPCState.Live:
            triggerName = liveTriggerName;
            break;
        case NPCState.Dead:
            triggerName = deadTriggerName;
            break;
        default:
            return; // Normal 状态不再触发任何 trigger
    }
        
        foreach (Animator animator in synchronizedAnimators)
        {
            if (animator != null)
            {
                // 重置所有触发器
                animator.ResetTrigger(deadTriggerName);
                animator.ResetTrigger(liveTriggerName);
                
                // 设置新的触发器
                animator.SetTrigger(triggerName);
                
                if (showDebugMessages)
                    Debug.Log($"[NarrativeManager] 设置 {animator.gameObject.name} 触发器: {triggerName}");
            }
        }
    }

    // ✅ 修改：更新所有NPC Animator的方法
    private void UpdateAllNPCAnimators()
    {
        if (synchronizedAnimators.Count == 0)
        {
            if (showDebugMessages)
                Debug.LogWarning("[NarrativeManager] 没有找到需要同步的Animator");
            return;
        }
        
        if (showDebugMessages)
            Debug.Log($"[NarrativeManager] 开始更新所有NPC Animator状态: {currentNPCState}");
        
        // 使用协程来延迟设置触发器，确保ResetTrigger完成
        StartCoroutine(UpdateAnimatorsWithDelay());
    }
    
    // ✅ 新增：延迟设置动画状态的协程
    private IEnumerator UpdateAnimatorsWithDelay()
    {
        // 先重置所有触发器
        foreach (Animator animator in synchronizedAnimators)
        {
            if (animator != null)
            {
                animator.ResetTrigger(deadTriggerName);
                animator.ResetTrigger(liveTriggerName);
            }
        }
        
        // 等待一帧确保重置完成
        yield return null;
        
        // 设置新的触发器
        SetTriggerForAllAnimators(currentNPCState);
    }
    
    // ✅ 保留原来的方法作为备用（只更新单个主要Animator）
    private void UpdateNPCAnimator()
    {
        if (npcAnimator == null) return;
        
        // 重置所有触发器
        npcAnimator.ResetTrigger(deadTriggerName);
        npcAnimator.ResetTrigger(liveTriggerName);
        
        // 根据当前 NPC 状态设置对应的触发器
        switch (currentNPCState)
        {
            case NPCState.Live:
                npcAnimator.SetTrigger(liveTriggerName);
                break;
                
            case NPCState.Dead:
                npcAnimator.SetTrigger(deadTriggerName);
                break;
        }
    }
    
    
    // 重置叙事系统状态
    public void ResetNarrativeSystem()
    {
        firstInteractionGift = false;
        secondInteractionGift = false;
        firstInteractionCompleted = false;
        secondInteractionCompleted = false;
        skyboxActive = false;
        currentNPCState = NPCState.Normal;
        endingTriggered = false;
        if (fakeSkybox != null)
        {
            fakeSkybox.SetActive(false);
        }
        skyboxActive = false;
    
        // ✅ 重置时也更新所有Animator
        UpdateAllNPCAnimators();
        
        Debug.Log("叙事系统已重置");
    }
    
    // 获取当前系统状态
    public string GetSystemStatus()
    {
        return $"第一次交互: 完成={firstInteractionCompleted}, 给礼物={firstInteractionGift}\n" +
               $"第二次交互: 完成={secondInteractionCompleted}, 给礼物={secondInteractionGift}\n" +
               $"Skybox状态: {skyboxActive}\n" +
               $"NPC状态: {currentNPCState}\n" +
               $"结局已触发: {endingTriggered}\n" +
               $"同步的Animator数量: {synchronizedAnimators.Count}";
    }

    private void OnEnable()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // 场景加载完成后调用
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 恢复引用
        RestoreReferences();
    }
    
    // ✅ 修改：恢复引用的方法
    private void RestoreReferences()
    {
        // 恢复 Skybox 引用
        if (fakeSkybox == null)
        {
            GameObject skyboxObj = GameObject.FindGameObjectWithTag(skyboxTag);
            if (skyboxObj != null)
            {
                fakeSkybox = skyboxObj;

                // ✅ 无论状态记录如何，初始化后就强制关闭
                fakeSkybox.SetActive(false);
                skyboxActive = false;

                if (showDebugMessages)
                    Debug.Log("[NarrativeManager] 已找到 fakeSkybox，并设为不激活");
            }
            else
            {
                Debug.LogWarning("[NarrativeManager] 未找到带有 fakeSkybox 标签的对象！");
            }
        }
        
        // ✅ 重新查找所有同步的Animator
        FindAllSynchronizedAnimators();
        
        // 恢复主要 NPC Animator 引用（如果还没有的话）
        if (npcAnimator == null && synchronizedAnimators.Count > 0)
        {
            npcAnimator = synchronizedAnimators[0];
            if (showDebugMessages)
                Debug.Log("[NarrativeManager] 已将第一个找到的Animator设为主要NPC Animator");
        }
        
        // 如果找到了任何 Animator，重新应用当前状态
        if (synchronizedAnimators.Count > 0)
        {
            if (showDebugMessages)
                Debug.Log($"[NarrativeManager] 当前状态: {currentNPCState}，准备同步所有Animator");
            UpdateAllNPCAnimators();
        }
    }
    
    // ✅ 新增：公共方法，允许外部手动刷新Animator列表
    public void RefreshAnimatorList()
    {
        FindAllSynchronizedAnimators();
        if (showDebugMessages)
            Debug.Log($"[NarrativeManager] 手动刷新完成，找到 {synchronizedAnimators.Count} 个Animator");
    }
    
    // ✅ 新增：公共方法，允许外部获取当前同步的Animator数量
    public int GetSynchronizedAnimatorCount()
    {
        return synchronizedAnimators.Count;
    }
}

