using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 引入场景管理命名空间

public class NarrativeManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static NarrativeManager Instance { get; private set; }
    public GameObject fakeSkybox;
    public Animator npcAnimator;  // 在 Inspector 中拖入 NPC 的 Animator 组件
    public string deadTriggerName = "Dead";
    public string liveTriggerName = "Live";
    public string resetTriggerName = "Reset";

    
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
    
    // 使用 UpdateNPCAnimator 更新动画状态
    UpdateNPCAnimator();
    
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
    
    // 更新 NPC 动画状态
    if (npcAnimator != null)
    {
        switch (currentNPCState)
        {
            case NPCState.Live:
                npcAnimator.SetTrigger("Live");
                break;
            case NPCState.Dead:
                npcAnimator.SetTrigger("Dead");
                // 或者使用布尔参数
                // npcAnimator.SetBool("IsDead", true);
                break;
            default:
                break;
        }
    }
    
    Debug.Log($"触发结局: {endingType}, NPC状态: {currentNPCState}, Skybox状态: {skyboxActive}");
    
    // 触发结局事件，让其他系统响应
    if (OnEndingTriggered != null)
    {
        OnEndingTriggered(endingType);
    }
}

    private void UpdateNPCAnimator()
{
    if (npcAnimator == null) return;
    
    // 重置所有触发器
    npcAnimator.ResetTrigger(deadTriggerName);
    npcAnimator.ResetTrigger(liveTriggerName);
    npcAnimator.ResetTrigger(resetTriggerName);
    
    // 根据当前 NPC 状态设置对应的触发器
    switch (currentNPCState)
    {
        case NPCState.Live:
            npcAnimator.SetTrigger(liveTriggerName);
            break;
            
        case NPCState.Dead:
            npcAnimator.SetTrigger(deadTriggerName);
            break;
            
        case NPCState.Normal:
            npcAnimator.SetTrigger(resetTriggerName);
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
    
        
        Debug.Log("叙事系统已重置");
    }
    
    // 获取当前系统状态
    public string GetSystemStatus()
    {
        return $"第一次交互: 完成={firstInteractionCompleted}, 给礼物={firstInteractionGift}\n" +
               $"第二次交互: 完成={secondInteractionCompleted}, 给礼物={secondInteractionGift}\n" +
               $"Skybox状态: {skyboxActive}\n" +
               $"NPC状态: {currentNPCState}\n" +
               $"结局已触发: {endingTriggered}";
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
    
    // 恢复引用的方法
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
        
        // 恢复 NPC Animator 引用
        if (npcAnimator == null)
        {
            GameObject npcObj = GameObject.FindWithTag(npcAnimatorTag);
            if (npcObj != null)
            {
                npcAnimator = npcObj.GetComponent<Animator>();
                
                // 如果找到了 Animator，重新应用当前状态
                if (npcAnimator != null)
                {
                    // 根据当前状态设置动画
                    UpdateNPCAnimator();
                }
            }
        }
    }
}

