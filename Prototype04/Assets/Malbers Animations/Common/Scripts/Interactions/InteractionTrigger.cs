using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MalbersAnimations.Controller;
using Cinemachine; // 添加Cinemachine命名空间
using UnityEngine.Events; // 添加UnityEvents命名空间

// 添加Icon以便在Inspector中更容易识别
[AddComponentMenu("Interaction/Dialogue Trigger")]
[RequireComponent(typeof(Collider))] // 确保对象有碰撞体
public class InteractionTrigger : MonoBehaviour
{
    [Header("一次性交互")]
    [Tooltip("是否为一次性交互")]
    public bool isOneTimeInteraction = false;

    [Tooltip("是否已经完成交互")]
    private bool interactionCompleted = false;

    [Tooltip("是否正在交互中")]
    private bool isInteracting = false;

    [Header("冷却时间设置")]
    [Tooltip("冷却时间 防止交互重叠")]
    [SerializeField]
    private float exitCooldownDuration = 1f; // 可以根据需要调整
    private bool isExitCooldown = false;

    [Tooltip("可交互的检测半径, 无功能 只是看到的范围 方便调试")] 
    public float interactionRadius = 2f;

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
    }

    [System.Serializable]
    public class DialogueChoice
    {
        [Tooltip("选项文本")]
        public string choiceText;
        
        [Tooltip("选择的按键")]
        public KeyCode choiceKey = KeyCode.Y;
        
        [Tooltip("选择后跳转到的消息索引")]
        public int jumpToMessageIndex = -1; // -1表示继续到下一条
        
        [Tooltip("选择后触发的事件")]
        public UnityEvent onChoiceSelected = new UnityEvent();

        [Tooltip("选择后执行的叙事动作")]
        public List<NarrativeAction> narrativeActions = new List<NarrativeAction>();
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
        [Range(0f, 5)]
        public float textToBlackScreenDelay = 0.3f;
        
        [Tooltip("黑屏淡入 到 黑屏淡出延迟")]
        [Range(0f, 5f)]
        public float blackScreenToFadeOutDelay = 0.2f;  
    }


public ExitAnimationDelays exitDelays = new ExitAnimationDelays();

    // Unity界面图标，便于在场景中识别
    private void OnDrawGizmos()
    {
        Gizmos.color = playerInRange ? Color.yellow: Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        
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
        
        // 获取重生管理器
        respawner = MRespawner.instance;
    }

  private void Update()
{
     if (isPlayerDead || isExitCooldown)
        return;
        
    if (playerInRange && !isFading)
    {
        // 获取当前消息（如果在交互中）
        DialogueMessage currentMessage = null;
        if (isInteracting && enableDialogue && currentMessageIndex < dialogueSettings.messages.Count)
        {
            currentMessage = dialogueSettings.messages[currentMessageIndex];
        }
        
        // 交互键逻辑
        if (Input.GetKeyDown(dialogueSettings.interactionKey))
        {
            // 如果是一次性交互且已完成，不再触发
            if (isOneTimeInteraction && interactionCompleted)
                return;
                
            // 如果正在交互中
            if (isInteracting)
            {
                // 检查当前消息是否是选择消息
                if (enableDialogue && currentMessage != null && currentMessage.isChoice)
                {
                    // 如果是选择消息，按X键不做任何事
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
                    // 添加：如果对话功能关闭，直接退出交互
                    ExitInteraction();
                }
            }
            // 否则开始交互
            else
            {
                HandleInteraction();
            }
        }
        
        // 检查选择按键 (如果正在显示选择)
        if (isInteracting && enableDialogue && currentMessage != null && currentMessage.isChoice)
        {
            foreach (var choice in currentMessage.choices)
            {
                if (Input.GetKeyDown(choice.choiceKey))
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
        isInteracting = true;
        
        // 当玩家按下交互按钮时激活相机
        if (enableCamera && cameraSettings.virtualCamera != null)
        {
            ActivateCamera(true);
        }
        

       if (enableDialogue && dialogueSettings.messages.Count > 0)
        {
            currentMessageIndex = 0;
            DisplayCurrentMessage();
        }
        else
        {
            // ✅ 如果只是打开了相机或空对白，也应该淡出提示文字
            if (!string.IsNullOrEmpty(promptSettings.promptMessage))
            {
                StartCoroutine(FadeText("", TextType.Prompt));
            }
            {
                StartCoroutine(FadeText("", TextType.Prompt));
            }

            if (dialogueSettings.backgroundImage != null)
            {
                StartCoroutine(FadeTextBackground(0f));
            }
        }
    }



    private void ExitInteraction()
    {
        isInteracting = false;
        isExitCooldown = true; // 进入冷却状态
        
        // 标记交互已完成(对于一次性交互)
        if (isOneTimeInteraction)
        {
            interactionCompleted = true;
        }
        
        StartCoroutine(CoordinatedExitAnimation());

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
    
    // 添加第一个可调整的延迟
    yield return new WaitForSeconds(exitDelays.textToBlackScreenDelay);
    
    // 2. 相机切换和黑屏淡入
    if (enableCamera && cameraSettings.virtualCamera != null)
    {
        // 淡入UI背景（黑屏）
        StartCoroutine(FadeUIBackground(true));
        
        // 等待UI背景淡入完成
        yield return new WaitForSeconds(fadeSettings.uiFadeInDuration);
        
        // 切换相机
        cameraSettings.virtualCamera.Priority = 0;
        
        // 添加第二个可调整的延迟
        yield return new WaitForSeconds(exitDelays.blackScreenToFadeOutDelay);
        
        // 淡出UI背景（黑屏）
        StartCoroutine(FadeUIBackground(false));
        
        // 等待UI背景淡出完成
        yield return new WaitForSeconds(fadeSettings.uiFadeOutDuration);
    }
    
    // 添加最终冷却延迟
    yield return new WaitForSeconds(exitCooldownDuration);
    
    // 冷却结束，如果玩家仍在范围内，显示交互提示
    if (playerInRange && (!isOneTimeInteraction || !interactionCompleted))
    {
        // 显示交互提示
        StartCoroutine(FadeText(promptSettings.promptMessage, TextType.Prompt));
        
// ✅ 只要有提示，就显示背景
    if (!string.IsNullOrEmpty(promptSettings.promptMessage) && dialogueSettings.backgroundImage != null)
    {
        StartCoroutine(FadeTextBackground(1.0f));
    }
    }
    
    isExitCooldown = false;
}

    private void HandleChoice(DialogueChoice choice)
    {
        // 1. 先触发UI/反馈相关事件
        choice.onChoiceSelected.Invoke();
        
        // 2. 然后执行叙事动作(如果有)

        foreach (var action in choice.narrativeActions)
        {
            if (action != null)
            {
                action.ExecuteAction();
            }
        }
        
        
        // 3. 最后处理对话流程
        if (choice.jumpToMessageIndex >= 0 && 
            choice.jumpToMessageIndex < dialogueSettings.messages.Count)
        {
            currentMessageIndex = choice.jumpToMessageIndex;
        }
        else
        {
            currentMessageIndex = (currentMessageIndex + 1) % 
                                dialogueSettings.messages.Count;
        }
        
        DisplayCurrentMessage();

        //on choiceSelected.Invoke(); // 触发选择事件 角色动画 音效等
        //Narrative Action 只负责叙事 结局 等等
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

    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag(playerTag) && !isPlayerDead)
    {
        playerInRange = true;
        
        // 如果是一次性交互且已完成，不做任何事
        if (isOneTimeInteraction && interactionCompleted)
            return;
                
        // 如果没有在交互中且不在冷却状态中，显示提示
        if (!isInteracting && !isExitCooldown)
        {
            StartCoroutine(FadeText(promptSettings.promptMessage, TextType.Prompt));

            // ✅ 改成无论是否启用对话，都显示背景（只要有提示就显示）
            if (dialogueSettings.backgroundImage != null)
            {
                StartCoroutine(FadeTextBackground(1.0f));
            }
        }
    }
}

    // 修改OnTriggerExit方法
    private void OnTriggerExit(Collider other)
{
    if (other.CompareTag(playerTag) && !isPlayerDead)
    {
        playerInRange = false;
        
        // 如果未开始交互，清除提示
        if (!isInteracting)
        {
            // 淡出文本
            StartCoroutine(FadeText("", TextType.Prompt));
            
            // 确保文本背景淡出
            if (dialogueSettings.backgroundImage != null)
            {
                StartCoroutine(FadeTextBackground(0f));
            }
        }
        else
        {
            // 如果正在交互中，退出交互
            ExitInteraction();
        }
        
        // 关闭相机
        if (enableCamera && cameraSettings.virtualCamera != null)
        {
            cameraSettings.virtualCamera.Priority = 0;
        }
    }
}

    private enum TextType
    {
        Prompt,
        Dialogue
    }

private IEnumerator FadeText(string newText, TextType textType = TextType.Dialogue)
{
    // 检查CanvasGroup是否存在
    if (textCanvasGroup == null) yield break;
    
    // 如果是对话类型且对话功能关闭，则退出
    // 但允许显示提示类型的文本
    if (textType == TextType.Dialogue && !enableDialogue) yield break;
    
    isFading = true;
    
    // 根据文本类型选择淡出时间
    float fadeOutDuration = (textType == TextType.Prompt) 
        ? promptSettings.promptFadeOutDuration 
        : dialogueSettings.dialogueFadeOutDuration;
    
    // 淡出当前文本
    float startAlpha = textCanvasGroup.alpha;
    for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
    {
        textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeOutDuration);
        yield return null;
    }
    textCanvasGroup.alpha = 0f;
    
    // 更新文本内容
    dialogueSettings.dialogueText.text = newText;
    
    // 如果新文本为空，不需要淡入
    if (string.IsNullOrEmpty(newText))
    {
        isFading = false;
        yield break;
    }
    
    // 根据文本类型选择淡入时间
    float fadeInDuration = (textType == TextType.Prompt) 
        ? promptSettings.promptFadeInDuration 
        : dialogueSettings.dialogueFadeInDuration;
    
    // 淡入新文本
    for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
    {
        textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
        yield return null;
    }
    textCanvasGroup.alpha = 1f;
    
    isFading = false;
}

    private IEnumerator FadeTextBackground(float targetAlpha)
    {
        if (dialogueSettings.backgroundImage == null) yield break;
        
        float startAlpha = dialogueSettings.backgroundImage.color.a;
        float duration = targetAlpha > 0 ? fadeSettings.textBackgroundFadeInDuration : fadeSettings.textBackgroundFadeOutDuration;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            Color bgColor = dialogueSettings.backgroundImage.color;
            bgColor.a = newAlpha;
            dialogueSettings.backgroundImage.color = bgColor;
            yield return null;
        }
        
        Color finalColor = dialogueSettings.backgroundImage.color;
        finalColor.a = targetAlpha;
        dialogueSettings.backgroundImage.color = finalColor;
    }
    
    private IEnumerator FadeUIBackground(bool fadeIn)
    {
        if (fadeSettings.uiBackgroundImage == null) yield break;
        
        float startAlpha = fadeSettings.uiBackgroundImage.color.a;
        float targetAlpha = fadeIn ? fadeSettings.uiMaxAlpha : 0f;
        float duration = fadeIn ? fadeSettings.uiFadeInDuration : fadeSettings.uiFadeOutDuration;
        
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

    // 公共方法，可由其他脚本调用
    public void OnPlayerDeath()
    {
        // 先设置死亡状态
        isPlayerDead = true;
        playerInRange = false;
        
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
    }
}