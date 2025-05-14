using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MalbersAnimations.Controller;
using Cinemachine; // 添加Cinemachine命名空间
using UnityEngine.Events;
using UnityEngine.Playables; // 添加Playable命名空间
using MalbersAnimations; // 添加UnityEvents命名空间

// 添加Icon以便在Inspector中更容易识别
[AddComponentMenu("Interaction System")]
[RequireComponent(typeof(Collider))] // 确保对象有碰撞体
public class InteractionTrigger : MonoBehaviour
{
    
    public ResourceManager resourceManager;  // 拖到Inspector
    private GameObject playerExtraLogic;
    [Header("对话音效设置")]
    [Tooltip("进入对话时启用，结束后禁用的音效物体（应包含 FMOD StudioEventEmitter）")]
    public GameObject dialogueSFXObject;
    public float dialogueSFXDelay = 0f;
    
    private bool pendingCompleteAfterExit = false;
    private enum InteractionState
    {
        Ready,       // 准备交互状态
        Active,      // 正在交互中
        Cooldown,    // 冷却状态
        Completed    // 已完成（一次性交互）
    }

    [Header("一次性交互")]
    [Tooltip("是否为一次性交互")]
    public bool isOneTimeInteraction = false;

    [Header("冷却时间设置")]
    [Tooltip("冷却时间 防止交互重叠")]
    [SerializeField]
    private float exitCooldownDuration = 1f; // 可以根据需要调整


    [Header("双碰撞体设置")]
    [Tooltip("是否使用独立的过场动画触发区域")]
    public bool useSeparateCutsceneTrigger = true;

    [Tooltip("过场动画触发碰撞体")]
    public BoxCollider cutsceneTriggerCollider;

    [Tooltip("对话/交互触发碰撞体")]
    public CapsuleCollider dialogueTriggerCollider;
    [Header("专属存档触发器")]
public Collider saveTriggerCollider;
private bool playerInSaveRange = false;

    private bool playerInCutsceneRange = false;


    [Tooltip("当前交互状态")]
    private InteractionState currentState = InteractionState.Ready;
    
    [System.Serializable]  // Add this attribute to make the class serializable
    public class InteractionEvents
    {
        [Header("交互事件")]
        [Tooltip("交互开始时触发的事件")]
        public UnityEvent onInteractionStarted = new UnityEvent();
        
        [Tooltip("交互结束时触发的事件")]
        public UnityEvent onInteractionEnded = new UnityEvent();
        
        [Tooltip("玩家进入交互范围时触发的事件")]
        public UnityEvent onPlayerEnterRange = new UnityEvent();
        
        [Tooltip("玩家离开交互范围时触发的事件")]
        public UnityEvent onPlayerExitRange = new UnityEvent();
    }

    // 然后在InteractionTrigger类中添加该类的实例
    [Space(10)]
    [Header("事件系统")]
    [SerializeField]
    [Tooltip("交互事件设置")]
    public InteractionEvents events = new InteractionEvents();

    [System.Serializable]
    public class PromptSettings
    {
        [Tooltip("交互提示内容，例如：按X交互")]
        public string promptMessage = "按 X 交互";
        
        [SerializeField]
        [Tooltip("提示文本淡入时间")]
        public float promptFadeInDuration = 0.3f;
        
        [SerializeField]
        [Tooltip("提示文本淡出时间")]
        public float promptFadeOutDuration = 0.3f;

            [Tooltip("只有提示时，是否视为一次性交互")]
    public bool isPromptOnlyOneTime = false;  // ✅ 新增

        [Header("交互按键设置")]
    [Tooltip("主要交互按键")]
    public KeyCode mainInteractionKey = KeyCode.X;  // 默认使用X键
    
    [Tooltip("额外交互按键 (可选)")]
    public KeyCode alternativeInteractionKey = KeyCode.None;  // 默认不使用
  [Tooltip("额外交互按键 (可选)")]
    public string[] customInputValues = new string[] { "InputValue" };  // ← 添加这个数组

    }
    

    [Header("交互提示设置 (总是启用)")]
    public PromptSettings promptSettings = new PromptSettings();  // ✅ 加在这里


    [System.Serializable]
    public class DialogueSettings
    {  
          [Header("UI组件设置")]
    [Space(5)]
    [SerializeField] 
    public TextMeshProUGUI dialogueText;
    
    [SerializeField] 
    public Image backgroundImage;
    
    [Space(5)]
    [Header("对话内容设置")]
    [SerializeField]
    [Tooltip("对话文本淡入时间")]
    public float dialogueFadeInDuration = 0.5f;
    
    [SerializeField]
    [Tooltip("对话文本淡出时间")]
    public float dialogueFadeOutDuration = 0.5f;
    
    [SerializeField]
    public List<DialogueMessage> messages = new List<DialogueMessage>();
    
    [SerializeField] 
    public KeyCode interactionKey = KeyCode.X;
    }

    [System.Serializable]
    public class DialogueMessage
    {
    [TextArea(3, 10)]
    [Tooltip("对话文本内容")]
    public string text;
    
    [Tooltip("是否是选择消息")]
    public bool isChoice = false;
    
    [Tooltip("当这不是选择消息时，下一条消息的索引 (-1表示结束对话)")]
    public int nextMessageIndex = -1; // -1表示这是最后一条消息
    
    [Tooltip("当这是选择消息时，各个选项")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("资源检查设置")]
    public bool checkResources = false;
    
    [Tooltip("资源不足时跳转的对话索引")]
    public int noResourceDialogueIndex = -1;
    }

    [System.Serializable]
public class DialogueChoice
{
    [Tooltip("选项文本")]
    public string choiceText;
    
    [Tooltip("选择的按键")]
    public KeyCode choiceKey = KeyCode.Y;
    
    [Header("自定义输入设置")]
    [Tooltip("自定义输入名称列表（来自Unity Input Manager）")]
    public string[] customChoiceInputs = new string[0];  // ← 添加自定义输入数组
    
    [Tooltip("选择后跳转到的消息索引")]
    public int jumpToMessageIndex = -1; // -1表示继续到下一条
    
    [Tooltip("选择后触发的事件")]
    public UnityEvent onChoiceSelected = new UnityEvent();

    [Tooltip("选择后执行的叙事动作")]
    public List<NarrativeAction> narrativeActions = new List<NarrativeAction>();
    
    [Tooltip("是否消耗资源")]
    public bool consumeResources = false;
    
    [Tooltip("选择后是否直接结束对话")]
    public bool endDialogueAfterChoice = false;
    [Header("相机切换")]
public Cinemachine.CinemachineVirtualCamera targetCamera;
public bool switchCameraAfterChoice = true;

    [Header("资源不足处理")]
    [Tooltip("资源不足时执行的叙事动作")]
    public List<NarrativeAction> noResourceNarrativeActions = new List<NarrativeAction>();
    
    [Tooltip("资源不足时切换到的相机")]
    public Cinemachine.CinemachineVirtualCamera noResourceTargetCamera;
    
    [Tooltip("资源不足时是否切换相机")]
    public bool switchCameraOnNoResource = false;
    
    /// <summary>
    /// 检查是否按下了此选择的任何输入
    /// </summary>
    /// <returns>如果按下了选择键或任何自定义输入则返回true</returns>
    public bool IsInputPressed()
    {
        // 检查KeyCode按键
        if (choiceKey != KeyCode.None && Input.GetKeyDown(choiceKey))
        {
            return true;
        }
        
        // 检查自定义输入
        if (customChoiceInputs != null)
        {
            foreach (string inputName in customChoiceInputs)
            {
                if (!string.IsNullOrEmpty(inputName) && Input.GetButtonDown(inputName))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}

    [System.Serializable]
    public class CameraSettings
    {
        [Space(5)]
        [SerializeField] 
        [Tooltip("相机视角")]
        public CinemachineVirtualCamera virtualCamera;
    }
    
    [System.Serializable]
    public class FadeSettings
    {
        [Header("对话框设置")]
        [Space(5)]
        [SerializeField]
        [Tooltip("对话框淡入时间")]
        public float textBackgroundFadeInDuration = 0.5f;
        
        [SerializeField]
        [Tooltip("对话框淡出时间")]
        public float textBackgroundFadeOutDuration = 0.5f;
        
        [Header("黑屏UI背景设置")]
        [Space(5)]
        [SerializeField]
        [Tooltip("切换相机时的黑屏UI背景")]
        public Image uiBackgroundImage;
        
        [SerializeField]
        [Tooltip("黑屏UI背景淡入时间")]
        public float uiFadeInDuration = 0.5f;
        
        [SerializeField]
        [Tooltip("黑屏UI背景淡出时间")]
        public float uiFadeOutDuration = 0.5f;
        
        [SerializeField]
        [Tooltip("黑屏UI背景最大透明度")]
        [Range(0f, 1f)]
        public float uiMaxAlpha = 1f;
    }

        //[Header("玩家检测设置")]
        //[Tooltip("玩家Tag名称")
        //public string playerTag = "Animal";
        private string playerTag = "Animal";
        
        [Space(10)]
        [Header("对话功能")]
        [Tooltip("启用对话功能")]
        public bool enableDialogue = true;
        [SerializeField]
        [Tooltip("对话系统设置")]
        public DialogueSettings dialogueSettings = new DialogueSettings();
        
        [Space(10)]
        [Header("相机功能")]
        [Tooltip("启用相机功能")]
        public bool enableCamera = true;
        [SerializeField]
        [Tooltip("相机系统设置")]
        public CameraSettings cameraSettings = new CameraSettings();
        
        [Space(10)]
        [Header("淡入淡出效果")]
        [SerializeField]
        [Tooltip("淡入淡出设置")]
        public FadeSettings fadeSettings = new FadeSettings();

        // 私有变量
        private int currentMessageIndex = 0;
        private bool playerInRange = false;
        private bool isFading = false;
        private bool isPlayerDead = false;
        private MRespawner respawner;
        private CanvasGroup textCanvasGroup;

    [System.Serializable]
    public class ExitAnimationDelays
    {
        [Header("退出动画延迟设置")]
        [Space(5)]
        
        [Tooltip("文本/交互 淡出结束 到 黑屏淡入延迟")]
        [Range(0f, 10)]
        public float textToBlackScreenDelay = 0.3f;
        
        [Tooltip("黑屏淡入 到 黑屏淡出延迟")]
        [Range(0f, 5f)]
        public float blackScreenToFadeOutDelay = 0.2f;  
    }
[Space(10)]
[Header("过场动画功能")]
[Tooltip("启用过场动画功能")]
public bool enableCutscene = false;
[SerializeField]
[Tooltip("过场动画设置")]
public CutsceneSettings cutsceneSettings = new CutsceneSettings();

private bool hasCutscenePlayed = false; // 追踪过场动画是否已播放

[System.Serializable]
public class CutsceneSettings
{
    [Header("过场动画设置")]
    [Tooltip("过场动画Director组件")]
    public PlayableDirector cutsceneDirector;
    [Tooltip("触发延迟时间")]
    public float cutsceneDelay = 0.75f;

    [Header("相机设置")]
    [Tooltip("过场动画使用的所有虚拟相机")]
    public CinemachineVirtualCamera[] cutsceneVirtualCameras;

        [Tooltip("过场动画播放时虚拟相机的优先级")]
    public int cutsceneCameraPriority = 20;
    
    [Tooltip("过场动画结束后的虚拟相机优先级")]
    public int resetPriority = 0;
    
    [Tooltip("过场动画播放完成后是否自动显示交互提示")]
    public bool showPromptAfterCutscene = true;
    
    [Header("黑屏淡入淡出时间")]
    [Tooltip("黑屏淡入时间")]
    public float fadeInDuration = 0.5f;

    [Tooltip("黑屏淡出时间")]
    public float fadeOutDuration = 0.5f;
    public float blackScreenDuration = 1.5f; // 黑屏持续时间
    public float endingFadeInDuration = 0.5f; // 结束时的淡入时间
    public float endingFadeOutDuration = 0.5f; // 结束时的淡出时间
    
    [Header("事件")]
    [Tooltip("过场动画开始时触发的事件")]
    public UnityEvent onCutsceneStarted = new UnityEvent();
    
    [Tooltip("过场动画结束时触发的事件")]
    public UnityEvent onCutsceneCompleted = new UnityEvent();

    [Header("渐变效果")]
[Tooltip("是否启用过场动画的渐入渐出效果（对于没有设置摄像机的timeline可以关闭）")]
public bool enableFadeEffect = true;

[Tooltip("是否在过场动画播放期间禁用玩家输入")]
public bool disablePlayerInput = true;
}

[Header("存档功能")]
[Tooltip("启用存档功能")]
public bool enableSaveFunction = false;

[System.Serializable]
public class SaveSettings
{
    [Header("存档设置")]
        [Tooltip("交互后到黑屏淡入开始的延迟时间")]
    public float delayBeforeBlackScreen = 0.5f;
    [Tooltip("黑屏持续时间")]
    public float blackScreenDuration = 1.5f;
    
    
    [Tooltip("存档时触发的事件")]
    public UnityEvent onSaveCompleted = new UnityEvent();

        [Tooltip("是否仅在成功存档后激活检查点")]
    public bool activateCheckpointOnSaveOnly = true;

    [SerializeField]
    private MCheckPoint checkpointReference;

    // 添加公共访问器
    public MCheckPoint CheckpointReference { get { return checkpointReference; } }


}

[Header("物品检测系统")]
[Tooltip("SleepZoneHelper 组件，用于检测和处理玩家手中的物品")]
public SleepZoneHelper sleepZoneHelper;

[SerializeField]
public SaveSettings saveSettings = new SaveSettings();


public ExitAnimationDelays exitDelays = new ExitAnimationDelays();

private MalbersInput playerInput;
private Stats playerStats;






[Header("Malbers Zone集成")]
[Tooltip("用于触发存档动画的Zone")]
public Zone saveActionZone;

[Tooltip("物品放下后等待多久开始触发Zone (秒)")]
[Range(0.1f, 1.0f)]
public float dropItemDelay = 0.3f;

    // Unity界面图标，便于在场景中识别
    private void OnDrawGizmos()
    {
        // 显示一个对话图标
        Gizmos.DrawIcon(transform.position + Vector3.up, "console.infoicon", true);
    }

    private void Awake()
    {
        // 注册到UI管理器
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterInteraction(this);
        }
    }

    private void OnDestroy()
    {
        // 从UI管理器取消注册
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterInteraction(this);
        }
    }

    private void Start()
    {
        InitializeComponents();

            if (useSeparateCutsceneTrigger && cutsceneTriggerCollider != null && dialogueTriggerCollider != null)
    {
        // 确保两个碰撞体都设置为触发器
        cutsceneTriggerCollider.isTrigger = true;
        dialogueTriggerCollider.isTrigger = true;
    }
    }
    
    private void InitializeComponents()
    {
        // 初始化对话组件
    // ✅ 始终初始化提示/对话用的CanvasGroup
        if (dialogueSettings.dialogueText != null)
        {
            dialogueSettings.dialogueText.text = "";
            
            textCanvasGroup = dialogueSettings.dialogueText.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = dialogueSettings.dialogueText.gameObject.AddComponent<CanvasGroup>();
            }
            textCanvasGroup.alpha = 0f;
        }

        // ✅ 初始化背景（仍然保留enableDialogue判断，如果你希望非对话场景不显示背景可以跳过）
        if (dialogueSettings.backgroundImage != null)
        {
            Color bgColor = dialogueSettings.backgroundImage.color;
            bgColor.a = 0f;
            dialogueSettings.backgroundImage.color = bgColor;
        }
        
        // 初始化相机组件
        if (enableCamera && cameraSettings.virtualCamera != null)
        {
            // 初始化时将虚拟相机优先级设为0，这比SetActive(false)更符合Cinemachine的使用方式
            cameraSettings.virtualCamera.Priority = 0;
        }
        
        // 初始化UI背景
        if (fadeSettings.uiBackgroundImage != null)
        {
            // 确保UI背景一开始完全透明
            Color bgColor = fadeSettings.uiBackgroundImage.color;
            bgColor.a = 0f;
            fadeSettings.uiBackgroundImage.color = bgColor;
        }

            // 尝试自动获取玩家的MalbersInput组件
    if (playerInput == null)
        {
            // 获取主要动物/玩家
            var animal = MAnimal.MainAnimal;
            if (animal != null)
            {
                playerInput = animal.GetComponent<MalbersInput>();
                if (playerInput == null)
                {
                    //Debug.LogWarning("未能自动找到玩家的MalbersInput组件，可能会影响存档时的输入控制", this);
                }
                else
                {
                   // Debug.Log("成功获取到玩家输入组件: " + playerInput.name);
                }
                
                // 同时获取Stats组件
                playerStats = animal.GetComponent<Stats>();
                if (playerStats == null)
                {
                    //Debug.LogWarning("未能自动找到玩家的Stats组件，无法重置生命值和耐力值", this);
                }
                else
                {
                    //Debug.Log("成功获取到玩家属性组件: " + playerStats.name);
                }
            }
        }
    
        
        // 获取重生管理器
        respawner = MRespawner.instance;
    }

 private void Update()
{
    // 如果玩家死亡或者处于冷却状态，直接返回
    if (isPlayerDead || currentState == InteractionState.Cooldown)
        return;
        
    if (playerInRange && !isFading)
    {
        // 获取当前消息（如果在交互中）
        DialogueMessage currentMessage = null;
        if (currentState == InteractionState.Active && enableDialogue && currentMessageIndex < dialogueSettings.messages.Count)
        {
            currentMessage = dialogueSettings.messages[currentMessageIndex];
        }
        
        // 检查主要交互键或额外交互键
        bool interactionKeyPressed = Input.GetKeyDown(dialogueSettings.interactionKey) || 
                                   Input.GetKeyDown(promptSettings.mainInteractionKey) ||
                                   (promptSettings.alternativeInteractionKey != KeyCode.None && 
                                    Input.GetKeyDown(promptSettings.alternativeInteractionKey)) ||
                                   Input.GetButtonDown("Interact");  // 手柄支持
        
        // 交互键逻辑
        if (interactionKeyPressed)
        {
            // 如果是一次性交互且已完成，不再触发
            if (isOneTimeInteraction && currentState == InteractionState.Completed)
                return;
                
            // 如果正在交互中
            if (currentState == InteractionState.Active)
            {
                // 检查当前消息是否是选择消息
                if (enableDialogue && currentMessage != null && currentMessage.isChoice)
                {
                    // 如果是选择消息，按交互键不做任何事
                    return;
                }
                
                // 非选择消息，可以前进到下一条
                if (enableDialogue)
                {
                    DialogueMessage message = dialogueSettings.messages[currentMessageIndex];
                    
                    // 如果有指定下一条消息，前进到那条
                    if (message.nextMessageIndex >= 0 && message.nextMessageIndex < dialogueSettings.messages.Count)
                    {
                        currentMessageIndex = message.nextMessageIndex;
                        DisplayCurrentMessage();
                    }
                    else
                    {
                        // 没有更多对话，退出交互
                        ExitInteraction();
                    }
                }
                else
                {
                    // 如果对话功能关闭，直接退出交互
                    ExitInteraction();
                }
            }
            // 否则开始交互
            else if (currentState == InteractionState.Ready)
            {
                HandleInteraction();
            }
        }
        
        // 检查选择按键 (如果正在显示选择) - 使用新的检测方法
        if (currentState == InteractionState.Active && enableDialogue && currentMessage != null && currentMessage.isChoice)
        {
            foreach (var choice in currentMessage.choices)
            {
                if (choice.IsInputPressed())  // ← 使用新的方法检测输入
                {
                    HandleChoice(choice);
                    break;
                }
            }
        }
    }
}

    
private void HandleInteraction()
{
    // 如果是存档功能，直接调用存档处理函数
    if (enableSaveFunction)
    {
        HandleSave();
        return;
    }

    currentState = InteractionState.Active;
    events.onInteractionStarted.Invoke();

    // 只要进入正式交互模式就禁用玩家输入
    DisablePlayerInput();

    // 启动相机
    if (enableCamera && cameraSettings.virtualCamera != null)
    {
        ActivateCamera(true);
    }

    if (enableDialogue && dialogueSettings.messages.Count > 0)
    {
        currentMessageIndex = 0;
        StartCoroutine(DelayedDialogueSFX(dialogueSFXDelay));

        DisplayCurrentMessage();
    }
    else
    {
        // 如果只是Prompt(没有对话和相机)，直接启用玩家输入
        if (!enableCamera || cameraSettings.virtualCamera == null)
        {
            EnablePlayerInput();
        }

        // 淡出提示文本
        if (!string.IsNullOrEmpty(promptSettings.promptMessage))
        {
            StartCoroutine(FadeText("", TextType.Prompt));
        }

        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(0f));
        }

        // 关键逻辑更新：
        if (promptSettings.isPromptOnlyOneTime)
        {
            currentState = InteractionState.Completed;
        }
        else
        {
            StartCoroutine(CoordinatedExitAnimation());
        }
    }
}

private void ExitInteraction()
{
    // 切换到冷却状态
    currentState = InteractionState.Cooldown;
    
    if (pendingCompleteAfterExit)
    {
        currentState = InteractionState.Completed;
        pendingCompleteAfterExit = false; // 清除标记，避免影响下一次
    }

    // 不在这里启用输入，而是在协程中的适当时机启用
    StartCoroutine(CoordinatedExitAnimation());
    events.onInteractionEnded.Invoke();

    if (dialogueSFXObject != null)
    {
        dialogueSFXObject.SetActive(false);
    }
}


private IEnumerator CoordinatedExitAnimation()
{
    // 1. 如果启用了对话功能，淡出文本和背景
    if (enableDialogue)
    {
        // 同时开始文本和背景淡出
        StartCoroutine(FadeText("", TextType.Dialogue));

        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(0f));
        }

        // 等待较长的那个淡出时间完成
        float maxFadeOutTime = Mathf.Max(
            dialogueSettings.dialogueFadeOutDuration,
            fadeSettings.textBackgroundFadeOutDuration
        );
        yield return new WaitForSeconds(maxFadeOutTime);
    }
    else if (dialogueSettings.backgroundImage != null)
    {
        // 确保背景淡出即使对话功能未启用
        StartCoroutine(FadeTextBackground(0f));
        yield return new WaitForSeconds(fadeSettings.textBackgroundFadeOutDuration);
    }

    if (enableCamera && cameraSettings.virtualCamera != null)
    {
        // 添加第一个可调整的延迟
        yield return new WaitForSeconds(exitDelays.textToBlackScreenDelay);

        // 淡入UI背景（黑屏）
        StartCoroutine(FadeUIBackground(true));
        yield return new WaitForSeconds(fadeSettings.uiFadeInDuration);

        // 相机切换
        cameraSettings.virtualCamera.Priority = 0;

        // 启用输入
        EnablePlayerInput();

        // 添加第二个延迟
        yield return new WaitForSeconds(exitDelays.blackScreenToFadeOutDelay);

        // 淡出黑屏
        StartCoroutine(FadeUIBackground(false));
        yield return new WaitForSeconds(fadeSettings.uiFadeOutDuration);
    }
    else
    {
        // 没有相机切换，立即启用输入，不等待额外时间
        EnablePlayerInput();
    }

    // 如果有相机切换，再添加冷却延迟
    if (enableCamera && cameraSettings.virtualCamera != null)
    {
        yield return new WaitForSeconds(exitCooldownDuration);
    }

    // 冷却结束，检查状态
    if (currentState == InteractionState.Cooldown)
    {
        currentState = InteractionState.Ready; // 重置为准备状态
    }

    // 如果玩家仍在范围内，显示交互提示
if (playerInRange && (currentState != InteractionState.Completed))
{
    // 重置背景淡入淡出标志
    isBackgroundFading = false;
    
    UpdatePromptMessage();

    if (dialogueSettings.backgroundImage != null)
    {
        StartCoroutine(FadeTextBackground(1.0f));
    }
}
}

private void HandleChoice(DialogueChoice choice)
{
    DialogueMessage currentMessage = dialogueSettings.messages[currentMessageIndex];

    // 检查当前Message是否需要检查资源
    if (currentMessage.checkResources)
    {
        HandleResourceChoice(choice, currentMessage);
        return; // ✅ 重要：资源检查后直接返回，不继续执行下面的代码
    }
    
    // ✅ 只有不需要资源检查时才会执行到这里
    
    // 执行选择动作
    ExecuteChoiceActions(choice);

    // 检查是否需要切换到特定相机
    if (choice.targetCamera != null && choice.switchCameraAfterChoice)
    {
        StartCoroutine(SwitchToChoiceCameraThenExit(choice));
        return; // 防止继续走对话逻辑
    }

    // 检查是否直接结束对话
    if (choice.endDialogueAfterChoice)
    {
        ExitInteraction(); // 立即退出交互
        return; // 跳过后续流程
    }

    // 否则继续正常的对话流程
    JumpToNextMessage(choice);
    pendingCompleteAfterExit = true;
}

private IEnumerator SwitchToChoiceCameraThenExit(DialogueChoice choice)
{
    // 1. 淡出当前文本和背景
    if (enableDialogue)
    {
        StartCoroutine(FadeText("", TextType.Dialogue));
        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(0f));
        }
        
        // 等待文本淡出完成
        float maxFadeOutTime = Mathf.Max(
            dialogueSettings.dialogueFadeOutDuration,
            fadeSettings.textBackgroundFadeOutDuration
        );
        yield return new WaitForSeconds(maxFadeOutTime);
    }

    // 2. 使用对话系统的黑屏设置进行淡入
    if (fadeSettings.uiBackgroundImage != null)
    {
        // 使用dialogue的fade设置
        yield return StartCoroutine(FadeUIBackground(true, dialogueSettings.dialogueFadeInDuration + 1f));
    }

    // 3. 切换相机
    if (cameraSettings.virtualCamera != null)
        cameraSettings.virtualCamera.Priority = 0; // 原始相机降优先级

    if (choice.targetCamera != null)
        choice.targetCamera.Priority = 20; // 目标相机升优先级

    // 4. 等待一小段时间让相机切换完成，然后淡出黑屏
    yield return new WaitForSeconds(0.5f);
    
    if (fadeSettings.uiBackgroundImage != null)
    {
        // 使用dialogue的fade设置
        yield return StartCoroutine(FadeUIBackground(false, dialogueSettings.dialogueFadeOutDuration + 1f));
    }

    // 5. 执行叙事动作和事件
    ExecuteChoiceActions(choice);

    // 6. 直接标记为完成状态，不显示prompt
    currentState = InteractionState.Completed;
    events.onInteractionEnded.Invoke();
    EnablePlayerInput();

    // 7. 关闭音效（如果有）
    if (dialogueSFXObject != null)
    {
        dialogueSFXObject.SetActive(false);
    }
}

private void HandleResourceChoice(DialogueChoice choice, DialogueMessage currentMessage)
{
    // 只在消耗资源的选项时进行资源检查
    if (choice.consumeResources)
    {
        // 首先检查物品是否被拾取
        if (resourceManager != null && !resourceManager.isPickedUp)
        {
            Debug.Log("物品未被拾取，视为资源不足");

            // 执行资源不足 narrative actions
            if (choice.noResourceNarrativeActions != null && choice.noResourceNarrativeActions.Count > 0)
            {
                foreach (var action in choice.noResourceNarrativeActions)
                {
                    if (action != null) action.ExecuteAction();
                }
            }

            // 切换相机
            if (choice.noResourceTargetCamera != null && choice.switchCameraOnNoResource)
            {
                StartCoroutine(SwitchToNoResourceCameraThenExit(choice));
                return;
            }

            // 跳转到资源不足对话
            JumpToResourceFailureMessage(currentMessage);
            return;
        }
        // 然后检查资源是否充足
        else if (resourceManager != null && resourceManager.currentValue > 0)
        {
            // 资源充足，执行选择动作
            ExecuteChoiceActions(choice);

            // 先消耗资源
            ConsumeResources();

            // 检查是否需要切换到特定相机
            if (choice.targetCamera != null && choice.switchCameraAfterChoice)
            {
                StartCoroutine(SwitchToChoiceCameraThenExit(choice));
                return; // 防止继续走对话逻辑
            }

            // 消耗资源后，标记为完成状态，防止重新触发
            pendingCompleteAfterExit = true;

            // 然后检查是否需要直接退出对话
            if (choice.endDialogueAfterChoice)
            {
                // 如果直接退出对话，立即设置为完成状态
                currentState = InteractionState.Completed;
                ExitInteraction();
                return;
            }

            // 跳转到下一条消息
            JumpToNextMessage(choice);
        }
        else
        {
            // 资源不足的新处理逻辑
            Debug.Log("资源不足，执行特定的处理");
            
            // 执行资源不足时的叙事动作
            if (choice.noResourceNarrativeActions != null && choice.noResourceNarrativeActions.Count > 0)
            {
                Debug.Log($"执行 {choice.noResourceNarrativeActions.Count} 个资源不足时的叙事动作");
                foreach (var action in choice.noResourceNarrativeActions)
                {
                    if (action != null)
                    {
                        action.ExecuteAction();
                    }
                }
            }
            
            // 检查是否需要切换到资源不足时的特定相机
            if (choice.noResourceTargetCamera != null && choice.switchCameraOnNoResource)
            {
                // 使用原有的相机切换协程，但传入资源不足时的相机
                StartCoroutine(SwitchToNoResourceCameraThenExit(choice));
                return;
            }
            
            // 如果没有相机切换，跳转到资源不足对话（保持原有行为）
            JumpToResourceFailureMessage(currentMessage);
            return;
        }
    }
    else
    {
        // 不需要消耗资源的选项，直接执行
        ExecuteChoiceActions(choice);

        // 检查是否需要切换到特定相机
        if (choice.targetCamera != null && choice.switchCameraAfterChoice)
        {
            StartCoroutine(SwitchToChoiceCameraThenExit(choice));
            return; // 防止继续走对话逻辑
        }

        // 如果勾选了"选择后结束对话"，直接退出
        if (choice.endDialogueAfterChoice)
        {
            ExitInteraction();
            return;
        }

        JumpToNextMessage(choice);
    }

    // 成功完成交互才标记为一次性
    pendingCompleteAfterExit = true;
}

private IEnumerator SwitchToNoResourceCameraThenExit(DialogueChoice choice)
{
    // 1. 淡出当前文本和背景
    if (enableDialogue)
    {
        StartCoroutine(FadeText("", TextType.Dialogue));
        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(0f));
        }
        
        // 等待文本淡出完成
        float maxFadeOutTime = Mathf.Max(
            dialogueSettings.dialogueFadeOutDuration,
            fadeSettings.textBackgroundFadeOutDuration
        );
        yield return new WaitForSeconds(maxFadeOutTime);
    }

    // 2. 使用对话系统的黑屏设置进行淡入
    if (fadeSettings.uiBackgroundImage != null)
    {
        // 使用dialogue的fade设置
        yield return StartCoroutine(FadeUIBackground(true, dialogueSettings.dialogueFadeInDuration + 1f));
    }

    // 3. 切换相机（使用资源不足时的相机）
    if (cameraSettings.virtualCamera != null)
        cameraSettings.virtualCamera.Priority = 0; // 原始相机降优先级

    if (choice.noResourceTargetCamera != null)
        choice.noResourceTargetCamera.Priority = 20; // 目标相机升优先级

    // 4. 等待一小段时间让相机切换完成，然后淡出黑屏
    yield return new WaitForSeconds(0.5f);
    
    if (fadeSettings.uiBackgroundImage != null)
    {
        // 使用dialogue的fade设置
        yield return StartCoroutine(FadeUIBackground(false, dialogueSettings.dialogueFadeOutDuration + 1f));
    }

    // 5. 完成交互（资源不足不标记为完成，允许再次尝试）
    currentState = InteractionState.Completed;
    events.onInteractionEnded.Invoke();
    EnablePlayerInput();

}


// 辅助方法4 - 消耗资源
private void ConsumeResources()
{
    if (resourceManager != null)
    {
        Debug.Log("消耗资源");
        resourceManager.ConsumeAllResource();
    }
}

// 辅助方法5 - 执行选择的动作
private void ExecuteChoiceActions(DialogueChoice choice)
{
    // 触发选择事件
    choice.onChoiceSelected.Invoke();

    // 执行叙事动作
    foreach (var action in choice.narrativeActions)
    {
        if (action != null)
        {
            action.ExecuteAction();
        }
    }
}

// 辅助方法6 - 跳转到下一条消息
private void JumpToNextMessage(DialogueChoice choice)
{
    // 如果有指定的跳转目标
    if (choice.jumpToMessageIndex >= 0 && 
        choice.jumpToMessageIndex < dialogueSettings.messages.Count)
    {
        currentMessageIndex = choice.jumpToMessageIndex;
    }
    else
    {
        // 否则简单地前进到下一条消息
        currentMessageIndex = (currentMessageIndex + 1) % dialogueSettings.messages.Count;
    }

    // 显示当前消息
    DisplayCurrentMessage();
}

// 辅助方法7 - 跳转到资源不足时的消息
private void JumpToResourceFailureMessage(DialogueMessage currentMessage)
{
    if (currentMessage.noResourceDialogueIndex >= 0 &&
        currentMessage.noResourceDialogueIndex < dialogueSettings.messages.Count)
    {
        currentMessageIndex = currentMessage.noResourceDialogueIndex;
        DisplayCurrentMessage();
        
        // 添加明确标志，表示这是资源不足的情况
        pendingCompleteAfterExit = false; // 确保不会标记为完成
    }
    else
    {
        Debug.LogWarning("资源不足，但未设置 noResourceDialogueIndex");
        // 不要调用ExitInteraction()，而是直接结束当前对话，但不标记为完成
        StartCoroutine(EndDialogueWithoutCompletion());
    }
}

    private IEnumerator EndDialogueWithoutCompletion()
    {
        // 设置为冷却状态，但确保不会标记为完成
        currentState = InteractionState.Cooldown;
        pendingCompleteAfterExit = false; // 明确设置为false
        
        // 启动退出动画
        StartCoroutine(CoordinatedExitAnimation());
        events.onInteractionEnded.Invoke();
        
        // 等待足够时间让动画完成
        yield return new WaitForSeconds(3.0f); // 适当调整时间
        
        // 确保状态恢复为Ready而不是Completed
        if (currentState == InteractionState.Cooldown)
        {
            currentState = InteractionState.Ready;
            Debug.Log("对话已结束但可再次触发（资源不足）");
        }
    }
    private void ActivateCamera(bool activate)
    {
        if (cameraSettings.virtualCamera != null)
        {
            // 先执行UI背景淡入，然后再切换相机
            if (activate)
            {
                StartCoroutine(FadeUIBackground(true));
                
                // 延迟切换相机，等待UI背景淡入完成
                StartCoroutine(DelayedCameraActivation(true));
            }
            else
            {
                // 先淡入UI背景
                StartCoroutine(FadeUIBackground(true));
                
                // 延迟关闭相机，等待UI背景淡入完成
                StartCoroutine(DelayedCameraActivation(false));
            }
        }
    }

    private void DisplayCurrentMessage()
    {
        if (!enableDialogue || currentMessageIndex >= dialogueSettings.messages.Count)
            return;
                
        DialogueMessage message = dialogueSettings.messages[currentMessageIndex];
        
        if (message.isChoice)
        {
            // 构建选择文本 - 选项直接跟在消息后面
            System.Text.StringBuilder choiceBuilder = new System.Text.StringBuilder();
            choiceBuilder.Append(message.text);
            choiceBuilder.Append("\n"); // 添加一个空格
            
            // 添加所有选项，用 / 分隔
            bool isFirst = true;
            foreach (var choice in message.choices)
            {
                if (!isFirst)
                choiceBuilder.Append(" / ");
                choiceBuilder.Append(choice.choiceKey.ToString());
                isFirst = false;
            }
            
            StartCoroutine(FadeText(choiceBuilder.ToString()));
        }
        else
        {
            StartCoroutine(FadeText(message.text));
        }
        
        // 确保背景图像可见
        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(1f));
        }
    }
    
    private IEnumerator DelayedCameraActivation(bool activate)
    {
        // 等待UI背景淡入完成
        if (fadeSettings.uiBackgroundImage != null)
        {
            yield return new WaitForSeconds(fadeSettings.uiFadeInDuration);
        }
        
        // 切换相机
        if (cameraSettings.virtualCamera != null)
        {
            if (activate)
            {
                cameraSettings.virtualCamera.Priority = 20; // 提高优先级使其覆盖主相机
            }
            else
            {
                cameraSettings.virtualCamera.Priority = 0; // 降低优先级
            }
        }
        
        // 等待一小段时间，让相机切换完成
        yield return new WaitForSeconds(0.2f);
        
        // 然后淡出UI背景
        StartCoroutine(FadeUIBackground(false));
    }

private bool hasShownPrompt = false; // 跟踪是否已经显示过提示

private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag(playerTag) && !isPlayerDead)
    {
        var animator = other.GetComponentInParent<Animator>();
        if (animator == null || !animator.enabled)
            return;

        // 检查是否进入存档范围 - 修复这里的逻辑
        if (enableSaveFunction && saveTriggerCollider != null)
        {
            // 直接检查是否是存档触发器被触发
            if (saveTriggerCollider == GetComponent<Collider>())
            {
                // 如果当前对象的主碰撞体就是存档触发器
                playerInSaveRange = true;
                playerInRange = true; // 存档也需要设置playerInRange
                Debug.Log("玩家进入存档触发范围");

                if (currentState == InteractionState.Ready)
                {
                    events.onPlayerEnterRange.Invoke();
                    UpdatePromptMessage();
                    
                    if (dialogueSettings.backgroundImage != null)
                    {
                        StartCoroutine(FadeTextBackground(1.0f));
                    }
                }
                return; // 存档功能专用，直接返回
            }
        }

        // 检查是哪个碰撞体被触发
        bool inCutsceneRange = false;
        if (useSeparateCutsceneTrigger && cutsceneTriggerCollider != null)
        {
            // 检查是否是过场动画触发器
            if (other.bounds.Intersects(cutsceneTriggerCollider.bounds) || 
                GetComponent<Collider>() == cutsceneTriggerCollider)
            {
                inCutsceneRange = true;
                playerInCutsceneRange = true;
                Debug.Log("玩家进入过场动画范围");
                
                FindAndControlExtraLogic(false);
                
                // 检查是否需要播放过场动画
                if (enableCutscene && !hasCutscenePlayed)
                {
                    if (cutsceneSettings.disablePlayerInput)
                    {
                        DisablePlayerInput();
                    }
                    
                    StartCoroutine(PlayCutsceneSequence());
                    return;
                }
            }
        }
        
        // 对话范围检测
        bool inDialogueRange = false;
        if (dialogueTriggerCollider != null)
        {
            // 检查是否是对话触发器
            if (other.bounds.Intersects(dialogueTriggerCollider.bounds) || 
                GetComponent<Collider>() == dialogueTriggerCollider)
            {
                inDialogueRange = true;
            }
        }
        else if (!useSeparateCutsceneTrigger && !enableSaveFunction)
        {
            // 如果没有专门的对话碰撞体，且不是过场动画或存档，就使用默认的检测
            inDialogueRange = true; 
        }
        
        if (inDialogueRange && !playerInRange)
        {
            playerInRange = true;
            events.onPlayerEnterRange.Invoke();
            
            if (!playerInCutsceneRange)
            {
                FindAndControlExtraLogic(false);
            }
            
            if (isOneTimeInteraction && currentState == InteractionState.Completed)
                return;
                
            if (currentState == InteractionState.Ready)
            {
                isBackgroundFading = false;
                UpdatePromptMessage();
                
                if (dialogueSettings.backgroundImage != null)
                {
                    StartCoroutine(FadeTextBackground(1.0f));
                }
            }
        }
    }
}

    // 修改OnTriggerExit方法
private void OnTriggerExit(Collider other)
{
    if (other.CompareTag(playerTag) && !isPlayerDead)
    {
        var animator = other.GetComponentInParent<Animator>();
        if (animator == null || !animator.enabled)
            return;

if (enableSaveFunction && saveTriggerCollider != null && playerInSaveRange)
{
    // 检查触发的碰撞体是否是存档触发器
    if (other.GetComponent<Collider>() == saveTriggerCollider || GetComponent<Collider>() == saveTriggerCollider)
    {
        playerInSaveRange = false;
        playerInRange = false;
        Debug.Log("玩家离开存档触发范围");

        if (currentState == InteractionState.Ready)
        {
            // 停止所有可能正在进行的淡入淡出
            StopCoroutine(nameof(FadeText));
            StopCoroutine(nameof(FadeTextBackground));
            
            // 重置所有淡入淡出标志
            isBackgroundFading = false;
            isTextFading = false;
            isFading = false;
            
            events.onPlayerExitRange.Invoke();
            
            // 等待一帧确保协程完全停止
            StartCoroutine(DelayedPromptExit());
        }
        
        FindAndControlExtraLogic(true);
        return;
    }
}
        
        bool wasInCutsceneRange = playerInCutsceneRange;
        bool wasInDialogueRange = playerInRange;
        
        // 检查是否离开过场动画范围
        if (useSeparateCutsceneTrigger && cutsceneTriggerCollider != null)
        {
            bool stillInCutsceneRange = other.bounds.Intersects(cutsceneTriggerCollider.bounds);
            if (!stillInCutsceneRange && playerInCutsceneRange)
            {
                playerInCutsceneRange = false;
                Debug.Log("玩家离开过场动画范围");
                
                if (!playerInRange)
                {
                    FindAndControlExtraLogic(true);
                }
            }
        }
        
        // 检查是否离开对话范围
        bool stillInDialogueRange = false;
        if (dialogueTriggerCollider != null)
        {
            stillInDialogueRange = other.bounds.Intersects(dialogueTriggerCollider.bounds);
        }
        
        if (playerInRange && !stillInDialogueRange)
        {
            playerInRange = false;
            events.onPlayerExitRange.Invoke();
            
            if (!playerInCutsceneRange)
            {
                FindAndControlExtraLogic(true);
            }
            
            hasShownPrompt = false;

            if (currentState == InteractionState.Ready)
            {
                StartCoroutine(FadeText("", TextType.Prompt));
                
                if (dialogueSettings.backgroundImage != null)
                {
                    StartCoroutine(FadeTextBackground(0f));
                }
            }
            else if (currentState == InteractionState.Active)
            {
                ExitInteraction();
            }
            
            if (enableCamera && cameraSettings.virtualCamera != null)
            {
                cameraSettings.virtualCamera.Priority = 0;
            }
        }
        
        // 特殊情况处理：如果玩家同时离开了两个范围
        if (wasInCutsceneRange && wasInDialogueRange && !playerInCutsceneRange && !playerInRange)
        {
            FindAndControlExtraLogic(true);
        }
    }
}

private IEnumerator DelayedPromptExit()
{
    yield return null; // 等待一帧
    
    StartCoroutine(FadeText("", TextType.Prompt));
    
    if (dialogueSettings.backgroundImage != null)
    {
        StartCoroutine(FadeTextBackground(0f));
    }
}

// 辅助方法：延迟更新提示信息
private IEnumerator DelayedPromptUpdate()
{
    yield return null; // 等待一帧
    
    UpdatePromptMessage();
    
    if (dialogueSettings.backgroundImage != null)
    {
        StartCoroutine(FadeTextBackground(1.0f));
    }
}


private bool cutsceneEnded = false;

private IEnumerator PlayCutsceneSequence()
{
    yield return new WaitForSeconds(cutsceneSettings.cutsceneDelay);
    hasCutscenePlayed = true;
    currentState = InteractionState.Cooldown;

    bool inputWasDisabled = cutsceneSettings.disablePlayerInput;
    bool useFadeEffect = cutsceneSettings.enableFadeEffect;

    if (inputWasDisabled)
    {
        DisablePlayerInput();
    }

    if (useFadeEffect && fadeSettings.uiBackgroundImage != null)
    {
        StartCoroutine(FadeUIBackground(true, cutsceneSettings.fadeInDuration));
        yield return new WaitForSeconds(cutsceneSettings.fadeInDuration);
    }

    SetCutsceneCamerasPriority(cutsceneSettings.cutsceneCameraPriority);

    if (cutsceneSettings.cutsceneDirector != null)
    {
        cutsceneEnded = false;
        cutsceneSettings.cutsceneDirector.stopped += OnCutsceneStopped;

        cutsceneSettings.onCutsceneStarted.Invoke();
        cutsceneSettings.cutsceneDirector.Play();

        if (useFadeEffect && fadeSettings.uiBackgroundImage != null)
        {
            yield return new WaitForSeconds(cutsceneSettings.blackScreenDuration);
            StartCoroutine(FadeUIBackground(false, cutsceneSettings.fadeOutDuration));
        }

        // 等待 Timeline 播放真正结束
        while (!cutsceneEnded)
        {
            yield return null;
        }

        cutsceneSettings.cutsceneDirector.stopped -= OnCutsceneStopped;

        if (useFadeEffect && fadeSettings.uiBackgroundImage != null)
        {
            StartCoroutine(FadeUIBackground(true, cutsceneSettings.endingFadeInDuration));
            yield return new WaitForSeconds(cutsceneSettings.endingFadeInDuration);
            
            SetCutsceneCamerasPriority(0);
            yield return new WaitForSeconds(cutsceneSettings.blackScreenDuration);
        }

        if (useFadeEffect && fadeSettings.uiBackgroundImage != null)
        {
            StartCoroutine(FadeUIBackground(false, cutsceneSettings.endingFadeOutDuration));

            // 在 fade-out 开始时就恢复玩家控制
            if (inputWasDisabled)
            {
                EnablePlayerInput();
            }

            yield return new WaitForSeconds(cutsceneSettings.endingFadeOutDuration);
        }
        else
        {
            // 没有fade的情况下仍然恢复控制
            if (inputWasDisabled)
            {
                EnablePlayerInput();
            }
        }


        cutsceneSettings.onCutsceneCompleted.Invoke();
    }
    else
    {
        Debug.LogWarning("[Cutscene] 过场动画Director未设置！");
        if (useFadeEffect && fadeSettings.uiBackgroundImage != null)
        {
            StartCoroutine(FadeUIBackground(false, cutsceneSettings.fadeOutDuration));
            yield return new WaitForSeconds(cutsceneSettings.fadeOutDuration + 2.0f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }
    }

    if (inputWasDisabled)
    {
        EnablePlayerInput();
    }

    currentState = InteractionState.Ready;

    if (cutsceneSettings.showPromptAfterCutscene && playerInRange)
    {
        UpdatePromptMessage();

        if (dialogueSettings.backgroundImage != null)
        {
            StartCoroutine(FadeTextBackground(1.0f));
        }
    }

    Debug.Log("[Cutscene] 播放序列结束");
}

private void OnCutsceneStopped(PlayableDirector director)
{
    cutsceneEnded = true;
}

private void SetCutsceneCamerasPriority(int priority)
{
    if (cutsceneSettings.cutsceneVirtualCameras == null || cutsceneSettings.cutsceneVirtualCameras.Length == 0)
        return;
        
    foreach (var cam in cutsceneSettings.cutsceneVirtualCameras)
    {
        if (cam != null)
        {
            cam.Priority = priority;
            Debug.Log($"设置相机 {cam.name} 优先级为: {priority}");
        }
    }
}

public void ResetCutscene()
{
    hasCutscenePlayed = false;
}

private void FindAndControlExtraLogic(bool enable)
{
    // 获取当前主角色（每次都重新获取以适应重生情况）
    var animal = MAnimal.MainAnimal;
    if (animal != null)
    {
        // 查找Extra Logic
        Transform extraLogic = animal.transform.Find("Extra Logic");
        if (extraLogic != null)
        {
            // 更新引用
            playerExtraLogic = extraLogic.gameObject;
            
            // 设置激活状态
            playerExtraLogic.SetActive(enable);
            Debug.Log("找到并" + (enable ? "启用" : "禁用") + "了Extra Logic: " + playerExtraLogic.name);
        }
        else
        {
            Debug.LogWarning("在玩家对象中未找到Extra Logic游戏对象");
        }
    }
    else
    {
        Debug.LogWarning("未找到主角色");
    }
}
    

    private enum TextType
    {
        Prompt,
        Dialogue
    }

private IEnumerator DelayedAction(float delay, System.Action action)
{
    yield return new WaitForSeconds(delay);
    action?.Invoke();
}

private void HandleSave()
{
    // 检查玩家是否持有物品
    if (resourceManager != null && resourceManager.isPickedUp)
    {
        // 玩家持有物品，自动放下然后再存档
        if (sleepZoneHelper != null)
        {
            sleepZoneHelper.DropItem();
            // 短暂延迟后执行存档逻辑，给物品放下动画留出时间
            StartCoroutine(DelayedAction(dropItemDelay, ActivateZoneAndSave));
        }
        else
        {
            Debug.LogWarning("未找到SleepZoneHelper组件，无法自动放下物品");
            return; // 如果没有找到SleepZoneHelper，则不继续执行
        }
    }
    else
    {
        // 玩家没有持有物品，直接激活Zone和存档
        ActivateZoneAndSave();
    }
}

private void ActivateZoneAndSave()
{
    // 如果设置了saveActionZone，则先激活它
    if (saveActionZone != null)
    {
        // 获取当前的动物
        var animal = MAnimal.MainAnimal;
        if (animal != null)
        {
            // 激活Zone
            saveActionZone.ActivateZone(animal);
        }
    }
    
    // 无论Zone是否激活成功，都直接执行存档逻辑
    // 不再使用DelayedAction和zoneAnimationDelay
    ExecuteSaveLogic();
}

// 提取存档逻辑到单独的方法
private void ExecuteSaveLogic()
{
    currentState = InteractionState.Active;
    events.onInteractionStarted.Invoke();

    DisablePlayerInput();

    
    StartCoroutine(SaveGameCoroutine());
}

// 存档流程协程
// 存档流程协程 - 完整修复版本
private IEnumerator SaveGameCoroutine()
{
    // 停止所有正在进行的文本和背景淡入淡出
    StopCoroutine(nameof(FadeText));
    StopCoroutine(nameof(FadeTextBackground));
    
    // 重置所有淡入淡出标志
    isTextFading = false;
    isBackgroundFading = false;
    isFading = false;
    
    // 强制设置初始状态，确保从正确的透明度开始淡出
    if (textCanvasGroup != null && !string.IsNullOrEmpty(dialogueSettings.dialogueText.text))
    {
        textCanvasGroup.alpha = 1f; // 确保文本从完全不透明开始淡出
    }
    
    if (dialogueSettings.backgroundImage != null)
    {
        Color bgColor = dialogueSettings.backgroundImage.color;
        if (bgColor.a > 0)
        {
            bgColor.a = 1f; // 确保背景从完全不透明开始淡出
            dialogueSettings.backgroundImage.color = bgColor;
        }
    }
    
    // 同时开始文本和背景的淡出动画
    Coroutine textFadeOut = StartCoroutine(FadeText("", TextType.Prompt));
    Coroutine backgroundFadeOut = null;
    
    if (dialogueSettings.backgroundImage != null)
    {
        backgroundFadeOut = StartCoroutine(FadeTextBackground(0f));
    }
    
    // 等待两个淡出动画都完成
    yield return textFadeOut;
    if (backgroundFadeOut != null)
    {
        yield return backgroundFadeOut;
    }
    
    // 强制设置最终状态，确保完全透明
    if (textCanvasGroup != null)
    {
        textCanvasGroup.alpha = 0f;
        dialogueSettings.dialogueText.text = "";
    }
    
    if (dialogueSettings.backgroundImage != null)
    {
        Color bgColor = dialogueSettings.backgroundImage.color;
        bgColor.a = 0f;
        dialogueSettings.backgroundImage.color = bgColor;
    }
    
    // 然后继续存档流程
    yield return new WaitForSeconds(saveSettings.delayBeforeBlackScreen);
    
    // 淡入黑屏
    if (fadeSettings.uiBackgroundImage != null)
    {
        StartCoroutine(FadeUIBackground(true));
        yield return new WaitForSeconds(fadeSettings.uiFadeInDuration);
    }
    
    // 保持黑屏一段时间
    yield return new WaitForSeconds(saveSettings.blackScreenDuration);
    
    SaveGameData();
    
    // 触发存档完成事件
    saveSettings.onSaveCompleted.Invoke();
    
    // 淡出黑屏
    if (fadeSettings.uiBackgroundImage != null)
    {
        StartCoroutine(FadeUIBackground(false));
        yield return new WaitForSeconds(fadeSettings.uiFadeOutDuration);
    }
    
    EnablePlayerInput();
    
    // 切换到冷却状态
    currentState = InteractionState.Cooldown;
    
    // 如果是一次性交互，标记为已完成
    if (isOneTimeInteraction)
    {
        currentState = InteractionState.Completed;
        pendingCompleteAfterExit = false;
    }
    
    // 触发交互结束事件
    events.onInteractionEnded.Invoke();
    
    yield return new WaitForSeconds(exitCooldownDuration);
    
    // 只有当前状态仍然是冷却状态时才恢复为准备状态
    if (currentState == InteractionState.Cooldown)
    {
        currentState = InteractionState.Ready;
        
        // 玩家仍在范围内且不是完成状态，才显示交互提示
        if (playerInRange && (currentState != InteractionState.Completed))
        {
            // 重置标志，确保能正常执行
            isTextFading = false;
            isBackgroundFading = false;
            
            // 同时启动文本和背景淡入
            StartCoroutine(FadeText(promptSettings.promptMessage, TextType.Prompt));
            
            if (!string.IsNullOrEmpty(promptSettings.promptMessage) && dialogueSettings.backgroundImage != null)
            {
                StartCoroutine(FadeTextBackground(1.0f));
            }
        }
    }
}

private bool isTextFading = false;

// FadeText - 修复Prompt时间问题的完整版本
private IEnumerator FadeText(string newText, TextType textType = TextType.Dialogue)
{
    // 检查CanvasGroup是否存在
    if (textCanvasGroup == null)
    {
        Debug.LogWarning("[FadeText] 跳过: textCanvasGroup为空");
        yield break;
    }
    
    // 如果是对话类型且对话功能关闭，则退出
    if (textType == TextType.Dialogue && !enableDialogue)
    {
        Debug.LogWarning("[FadeText] 跳过: 对话类型但对话功能已关闭");
        yield break;
    }
    
    // 如果已经有淡入淡出效果在进行中，先强制停止
    if (isTextFading)
    {
        Debug.Log("[FadeText] 强制停止之前的文本淡入淡出");
        isTextFading = false;
        StopCoroutine(nameof(FadeText));
        yield return null; // 等待一帧确保协程完全停止
    }
    
    Debug.Log("[FadeText] 开始执行 - 类型: " + textType + ", 内容: " + newText);
    isTextFading = true;
    isFading = true;
    
    // 正确根据文本类型选择淡出时间
    float fadeOutDuration = (textType == TextType.Prompt) 
        ? promptSettings.promptFadeOutDuration    // ← 修复：使用Prompt的淡出时间
        : dialogueSettings.dialogueFadeOutDuration;
    
    Debug.Log("[FadeText] 开始淡出 - 持续时间: " + fadeOutDuration + " (类型: " + textType + ")");
    
    // 淡出当前文本
    float startAlpha = textCanvasGroup.alpha;
    for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
    {
        // 检查是否被中断
        if (!isTextFading)
        {
            Debug.Log("[FadeText] 淡出被中断");
            yield break;
        }
        
        textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeOutDuration);
        yield return null;
    }
    textCanvasGroup.alpha = 0f;
    Debug.Log("[FadeText] 淡出完成");
    
    // 更新文本内容
    dialogueSettings.dialogueText.text = newText;
    Debug.Log("[FadeText] 已更新文本内容");
    
    // 如果新文本为空，不需要淡入
    if (string.IsNullOrEmpty(newText))
    {
        Debug.Log("[FadeText] 文本为空，跳过淡入");
        textCanvasGroup.alpha = 0f; // 确保文本完全透明
    }
    else
    {
        // 正确根据文本类型选择淡入时间
        float fadeInDuration = (textType == TextType.Prompt) 
            ? promptSettings.promptFadeInDuration     // ← 修复：使用Prompt的淡入时间
            : dialogueSettings.dialogueFadeInDuration;
        
        Debug.Log("[FadeText] 开始淡入 - 持续时间: " + fadeInDuration + " (类型: " + textType + ")");
        
        // 淡入新文本
        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            // 检查是否被中断
            if (!isTextFading)
            {
                Debug.Log("[FadeText] 淡入被中断");
                yield break;
            }
            
            textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        textCanvasGroup.alpha = 1f;
        Debug.Log("[FadeText] 淡入完成");
    }
    
    // 无论文本是否为空，都确保标志位被重置
    isFading = false;
    isTextFading = false;
    Debug.Log("[FadeText] 协程完成，标志位已重置");
}
   private bool isBackgroundFading = false;

private IEnumerator FadeTextBackground(float targetAlpha)
{
    if (dialogueSettings.backgroundImage == null)
    {
        Debug.LogWarning("[FadeTextBackground] 跳过: backgroundImage为空");
        yield break;
    }
    
    // 如果已经有背景在淡入淡出中，先强制停止
    if (isBackgroundFading)
    {
        Debug.Log("[FadeTextBackground] 强制停止之前的背景淡入淡出");
        isBackgroundFading = false;
        StopCoroutine(nameof(FadeTextBackground));
        yield return null; // 等待一帧确保协程完全停止
    }
    
    Debug.Log("[FadeTextBackground] 开始执行 - 目标透明度: " + targetAlpha);
    isBackgroundFading = true;
    
    float startAlpha = dialogueSettings.backgroundImage.color.a;
    float duration = targetAlpha > 0 ? fadeSettings.textBackgroundFadeInDuration : fadeSettings.textBackgroundFadeOutDuration;
    
    Debug.Log("[FadeTextBackground] 开始淡入/淡出 - 起始透明度: " + startAlpha + ", 目标透明度: " + targetAlpha + ", 持续时间: " + duration);
    
    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        // 检查是否被中断
        if (!isBackgroundFading)
        {
            Debug.Log("[FadeTextBackground] 背景淡入/淡出被中断");
            yield break;
        }
        
        float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
        Color bgColor = dialogueSettings.backgroundImage.color;
        bgColor.a = newAlpha;
        dialogueSettings.backgroundImage.color = bgColor;
        yield return null;
    }
    
    // 确保达到目标透明度
    Color finalColor = dialogueSettings.backgroundImage.color;
    finalColor.a = targetAlpha;
    dialogueSettings.backgroundImage.color = finalColor;
    
    Debug.Log("[FadeTextBackground] 淡入/淡出完成");
    isBackgroundFading = false; // 确保标志被重置
}
        
private IEnumerator FadeUIBackground(bool fadeIn, float? customDuration = null)
{
    if (fadeSettings.uiBackgroundImage == null) yield break;

    float startAlpha = fadeSettings.uiBackgroundImage.color.a;
    float targetAlpha = fadeIn ? fadeSettings.uiMaxAlpha : 0f;

    // 🆕 使用 customDuration（如果没传就使用默认）
    float duration = customDuration ?? (fadeIn ? fadeSettings.uiFadeInDuration : fadeSettings.uiFadeOutDuration);

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
        Color bgColor = fadeSettings.uiBackgroundImage.color;
        bgColor.a = newAlpha;
        fadeSettings.uiBackgroundImage.color = bgColor;
        yield return null;
    }

    Color finalColor = fadeSettings.uiBackgroundImage.color;
    finalColor.a = targetAlpha;
    fadeSettings.uiBackgroundImage.color = finalColor;
}

    
    

private bool isPromptUpdating = false;

private void UpdatePromptMessage()
{
    // 如果已经在更新提示中，先强制停止之前的更新
    if (isPromptUpdating)
    {
        Debug.Log("[UpdatePromptMessage] 强制停止之前的提示更新");
        isPromptUpdating = false;
        StopCoroutine(nameof(FadePromptText));
    }
    
    isPromptUpdating = true;
    
    string baseMessage = promptSettings.promptMessage;
    
    // 当显示提示时使用这个更新后的信息
    string updatedPromptMessage = baseMessage;
    
    // 显示交互提示并使用协程跟踪完成状态
    StartCoroutine(FadePromptText(updatedPromptMessage));
    
    // 确保背景也显示
    if (dialogueSettings.backgroundImage != null)
    {
        // 重置背景淡入淡出标志，确保它可以正常执行
        isBackgroundFading = false;
        StartCoroutine(FadeTextBackground(1.0f));
        Debug.Log("[UpdatePromptMessage] 已启动背景淡入");
    }
}


private IEnumerator FadePromptText(string promptText)
{
    // 使用现有的FadeText方法
    yield return StartCoroutine(FadeText(promptText, TextType.Prompt));
    
    // 淡入淡出结束后重置标志
    isPromptUpdating = false;
    Debug.Log("[UpdatePromptMessage] 提示更新完成");
}

    // 公共方法，可由其他脚本调用
    public void OnPlayerDeath()
    {
        // 先设置死亡状态
        isPlayerDead = true;
        playerInRange = false;

        InteractionState previousState = currentState;
        currentState = InteractionState.Cooldown; // 暂时设置为冷却状态防止交互
        
        // 立即设置文本透明度为0（不使用协程）
        if (enableDialogue && textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
            dialogueSettings.dialogueText.text = "";
            
            // 立即设置背景透明度为0
            if (dialogueSettings.backgroundImage != null)
            {
                Color bgColor = dialogueSettings.backgroundImage.color;
                bgColor.a = 0f;
                dialogueSettings.backgroundImage.color = bgColor;
            }
        }
        
        // 立即关闭相机
        if (enableCamera && cameraSettings.virtualCamera != null)
        {
            cameraSettings.virtualCamera.Priority = 0;
        }
        
        // 立即隐藏UI背景
        if (fadeSettings.uiBackgroundImage != null)
        {
            Color bgColor = fadeSettings.uiBackgroundImage.color;
            bgColor.a = 0f;
            fadeSettings.uiBackgroundImage.color = bgColor;
        }
        
        // 停止所有可能正在进行的协程
        StopAllCoroutines();
        
        // 重置互动状态
        currentMessageIndex = 0;
        isFading = false;

        Debug.Log("Player is dead, disabling interaction UI.");
    }

    public void ResetDeathState()
    {
        isPlayerDead = false;
        if (currentState == InteractionState.Cooldown && !isOneTimeInteraction)
        {
            currentState = InteractionState.Ready;
        }
    }
    private void SaveGameData()
    {
        Debug.Log("正在保存游戏数据...");

            ResetPlayerStats();
        
        // 如果启用了检查点功能，设置检查点
        if (saveSettings.CheckpointReference != null)
        {
            // 设置MRespawner位置为当前交互点位置
            if (MRespawner.instance != null)
            {
                MRespawner.instance.transform.SetPositionAndRotation(transform.position, transform.rotation);
                
                // 设置重生状态为玩家当前状态
                var animal = MAnimal.MainAnimal;
                if (animal != null)
                {
                    MRespawner.instance.RespawnState = animal.ActiveStateID;
                }
            }
            
            // 手动触发检查点的OnEnter事件
            saveSettings.CheckpointReference.OnEnter.Invoke();
            
            // 设置为LastCheckPoint
            MCheckPoint.LastCheckPoint = saveSettings.CheckpointReference;
            
            // 禁用检查点碰撞体，避免再次触发
            if (saveSettings.CheckpointReference.Collider)
            {
                saveSettings.CheckpointReference.Collider.enabled = false;
            }
            
            Debug.Log("检查点已更新");
        }
    
    // 在这里添加JSON存档逻辑
    // ...
}

private void ResetPlayerStats()
{
    if (playerStats != null)
    {
        // 重置生命值 (ID为1)
        var healthStat = playerStats.Stat_Get(1);
        if (healthStat != null)
        {
            healthStat.Reset_to_Max();
            Debug.Log("已重置生命值至最大值");
        }
        else
        {
            Debug.LogWarning("未找到ID为1的生命值属性");
        }
        
        // 重置耐力值 (ID为2)
        var staminaStat = playerStats.Stat_Get(2);
        if (staminaStat != null)
        {
            staminaStat.Reset_to_Max();
            Debug.Log("已重置耐力值至最大值");
        }
        else
        {
            Debug.LogWarning("未找到ID为2的耐力值属性");
        }
    }
    else
    {
        Debug.LogWarning("未找到玩家Stats组件，无法重置生命值和耐力值");
    }
}

private void DisablePlayerInput()
{
    if (playerInput != null)
    {
        // 完全禁用MalbersInput组件
        playerInput.enabled = false;
        Debug.Log("已禁用所有玩家输入");
    }
    else
    {
        // 如果没有找到组件，尝试再次查找
        var animal = MAnimal.MainAnimal;
        if (animal != null)
        {
            playerInput = animal.GetComponent<MalbersInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                Debug.Log("重新查找并禁用玩家输入");
            }
            else
            {
                Debug.LogWarning("未能找到玩家的MalbersInput组件，无法禁用输入");
            }
        }
    }
}

private void EnablePlayerInput()
{
    if (playerInput != null)
    {
        // 重新启用MalbersInput组件
        playerInput.enabled = true;
        Debug.Log("已重新启用所有玩家输入");
    }
    else
    {
        Debug.LogWarning("未能找到玩家的MalbersInput组件，无法重新启用输入");
    }
}

private IEnumerator DelayedDialogueSFX(float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (dialogueSFXObject != null)
    {
        dialogueSFXObject.SetActive(true);

        var emitter = dialogueSFXObject.GetComponent<FMODUnity.StudioEventEmitter>();
        if (emitter != null && !emitter.IsPlaying())
        {
            emitter.Play();
        }
    }
}
}
