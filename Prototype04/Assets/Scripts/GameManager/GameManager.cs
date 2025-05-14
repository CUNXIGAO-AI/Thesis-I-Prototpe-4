using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Audio;

public class GameManager : MonoBehaviour
{
        public static GameManager Instance { get; private set; }
        private MAnimal playerAnimal;
        public ModeID SitModeID;  // 在 Inspector 拖你配置了坐下动作的 Mode，例如 Relax 或 SitMode
        private MInput playerInput;

    public TextMeshProUGUI startMessageText;
    public float textFadeDuration = 1f;
    private Coroutine textFadeCoroutine;
    public ThirdPersonFollowTarget cameraFollowTarget;
    public UnityEngine.UI.Image uiFadeImage;  // 在 Inspector 拖入 Image
    public float framefadeDuration = 1f;  // Fade 持续时间
    public UnityEngine.UI.Image secondUIFadeImage;  // 第二个UI fade image
    public float secondImageFadeDuration = 1f;  // 第二个image的fade持续时间
    private Coroutine secondImageFadeCoroutine;
    private Coroutine imageFadeCoroutine;
        [Header("Auto Restart Settings")]
    public float inactivityTimeThreshold = 10f;  // 默认设置为10秒，Inspector可改
    private float inactivityTimer = 0f;
    [Header("Camera Initial View Settings")]
[Range(-180f, 180f)] public float initialYaw = 0f;
[Range(-89f, 89f)] public float initialPitch = 10f;
[Header("Debug Preview (Runtime Only)")]
public bool previewCameraRotationInRuntime = false;
[Header("Fade Settings")]
public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

[Header("Camera Side Settings")]
[Tooltip("游戏开始时的相机侧向偏移值")]
public float initialCameraSide = 0.5f;
[Tooltip("侧向偏移恢复的平滑速度")]
public float sideOffsetLerpSpeed = 5f;


private float originalCameraSide; // 保存Inspector中设置的原始侧向偏移值
private float targetCameraSide;   // 目标侧向偏移值
private bool shouldRestoreCameraSide = false; // 控制是否应该恢复侧向偏移
private int playerInputCount = 0; // 记录输入次数
private bool cameraSideInitialized = false; // 标记相机侧向偏移是否已初始化

[Header("Camera Side Lerp Settings")]
public AnimationCurve cameraSideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

[Tooltip("从初始值 lerp 到目标值的总时间（秒）")]
public float cameraSideLerpDuration = 1.5f;
public float cameraDelay = 1f; // 延迟时间（秒）
private static bool hasPlayedStartSFX = false; // 确保只播放一次开始音效
private bool hasTextFadedOut = false; // 跟踪文本是否已经淡出


   [Header("Start Text Blink Settings")]
    [Tooltip("文本闪烁频率 (单位: 每秒周期数)")]
    [Range(0.0f, 10f)]
    public float blinkFrequency = 1f;
    
    [Tooltip("是否启用文本闪烁效果")]
    public bool enableBlinking = true;
    
    // 私有变量用于闪烁
    private Coroutine blinkCoroutine;
    private bool isBlinking = false;



         // 游戏状态枚举
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Ending
    }
    
    private GameState currentGameState = GameState.Playing;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: 场景加载完成，重新初始化 UI 引用");
        StartCoroutine(ReinitializeReferences());
    }

    private IEnumerator ReinitializeReferences()
    {
        yield return null;
        RefindUIComponents();
        RefindPlayerReferences();
        InitializeGameState();
        InitializeCameraSide();
    }

    private void InitializeGameState()
    {
        if (!UIManager.Instance.hasGameStarted && startMessageText != null)
        {
            // 直接开始闪烁，不需要先淡入
            if (enableBlinking)
            {
                StartBlinking();
            }
            else
            {
                // 如果不启用闪烁，则设置为完全不透明
                var c = startMessageText.color;
                c.a = 1f;
                startMessageText.color = c;
            }
        }
        
        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.lockInput = true;
        }
        
        SetCursorLock(true);
        
        if (!UIManager.Instance.hasGameStarted && playerAnimal != null && SitModeID != null)
        {
            playerAnimal.Mode_Activate(SitModeID, 8);
            Debug.Log("GameManager: 重新激活坐下模式");
        }
        
        if (playerInput != null)
        {
            playerInput.enabled = UIManager.Instance.hasGameStarted;
            Debug.Log($"GameManager: 玩家输入状态设置为 {UIManager.Instance.hasGameStarted}");
        }
    }

    // 开始闪烁效果
    private void StartBlinking()
    {
        if (startMessageText != null && !isBlinking)
        {
            isBlinking = true;
            blinkCoroutine = StartCoroutine(BlinkCoroutine());
        }
    }

    // 停止闪烁效果
    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        isBlinking = false;
    }

    // 闪烁协程
    private IEnumerator BlinkCoroutine()
    {
        while (isBlinking && !UIManager.Instance.hasGameStarted)
        {
            float cycleDuration = 1f / blinkFrequency;
            float halfCycle = cycleDuration / 2f;
            
            // 淡出到0
            yield return StartCoroutine(BlinkFade(1f, 0f, halfCycle));
            
            // 淡入到1
            yield return StartCoroutine(BlinkFade(0f, 1f, halfCycle));
        }
    }

    // 闪烁时的淡入淡出
    private IEnumerator BlinkFade(float startAlpha, float targetAlpha, float duration)
    {
        if (startMessageText == null) yield break;
        
        Color startColor = startMessageText.color;
        Color targetColor = startColor;
        startColor.a = startAlpha;
        targetColor.a = targetAlpha;
        
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float normalizedTime = t / duration;
            Color currentColor = Color.Lerp(startColor, targetColor, normalizedTime);
            startMessageText.color = currentColor;
            yield return null;
        }
        
        startMessageText.color = targetColor;
    }

    private void InitializeCameraSide()
    {
        if (cameraFollowTarget != null)
        {
            originalCameraSide = cameraFollowTarget.CameraSide;
            cameraFollowTarget.SetCameraSide(initialCameraSide);
            targetCameraSide = initialCameraSide;
            shouldRestoreCameraSide = false;
            cameraSideLerpTimer = 0f;
            cameraSideInitialized = true;
            Debug.Log($"GameManager: 相机侧向偏移初始化 - 设置为 {initialCameraSide}，原始值为 {originalCameraSide}");
        }
    }

    private bool hasTriggeredCameraLerp = false;
    public void OnPlayerInput()
    {
        if (!cameraSideInitialized || hasTriggeredCameraLerp) return;
        hasTriggeredCameraLerp = true;
        StartCoroutine(DelayedCameraSideLerp(cameraDelay));
    }

    private IEnumerator DelayedCameraSideLerp(float delay)
    {
        yield return new WaitForSeconds(delay);
        shouldRestoreCameraSide = true;
        targetCameraSide = 1.0f;
        cameraSideLerpTimer = 0f;
        Debug.Log("GameManager: 延迟结束，相机开始缓慢偏移到右侧");
    }

    private float cameraSideLerpTimer = 0f;

    private void UpdateCameraSide()
    {
        if (cameraFollowTarget != null && shouldRestoreCameraSide)
        {
            cameraSideLerpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(cameraSideLerpTimer / cameraSideLerpDuration);
            float curveValue = cameraSideCurve.Evaluate(t);
            float newSide = Mathf.Lerp(initialCameraSide, targetCameraSide, curveValue);
            cameraFollowTarget.SetCameraSide(newSide);

            if (t >= 1f)
            {
                shouldRestoreCameraSide = false;
                cameraSideLerpTimer = 0f;
                cameraFollowTarget.SetCameraSide(targetCameraSide);
                Debug.Log($"GameManager: 相机侧向偏移完成: {targetCameraSide}");
            }
        }
    }

    private void RefindUIComponents()
    {
        if (startMessageText == null)
        {
            GameObject startGameObj = GameObject.Find("Start Game");
            if (startGameObj != null)
            {
                startMessageText = startGameObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("GameManager: 重新找到 StartGame 文本组件");
            }
        }
        
        if (uiFadeImage == null)
        {
            GameObject startUIObj = GameObject.Find("Start UI");
            if (startUIObj != null)
            {
                uiFadeImage = startUIObj.GetComponent<UnityEngine.UI.Image>();
                Debug.Log("GameManager: 重新找到 Start UI 图片组件");
            }
        }
        
        if (cameraFollowTarget == null)
        {
            cameraFollowTarget = FindObjectOfType<ThirdPersonFollowTarget>();
            Debug.Log("GameManager: 重新找到相机控制器");
        }

        if (secondUIFadeImage == null)
    {
        GameObject secondUIObj = GameObject.Find("Title UI");
        if (secondUIObj != null)
        {
            secondUIFadeImage = secondUIObj.GetComponent<UnityEngine.UI.Image>();
            Debug.Log("GameManager: 重新找到 Second UI 图片组件");
        }
    }
    }

    private void RefindPlayerReferences()
    {
        if (MRespawner.instance != null)
        {
            GameObject player = MRespawner.instance.player;
            if (player != null)
            {
                playerAnimal = player.GetComponent<MAnimal>();
                playerInput = player.GetComponent<MInput>();
                Debug.Log("GameManager: 重新找到玩家组件");
            }
        }
    }

    void Start()
    {
        if (startMessageText == null)
        {
            startMessageText = GameObject.Find("Start Game")?.GetComponent<TextMeshProUGUI>();
        }

        if (uiFadeImage == null)
        {
            uiFadeImage = GameObject.Find("Start UI")?.GetComponent<UnityEngine.UI.Image>();
        }

        if (secondUIFadeImage == null)
{
    secondUIFadeImage = GameObject.Find("Second UI")?.GetComponent<UnityEngine.UI.Image>();
}

        if (cameraFollowTarget == null)
        {
            cameraFollowTarget = FindObjectOfType<ThirdPersonFollowTarget>();
        }

        if (!UIManager.Instance.hasGameStarted && startMessageText != null)
        {
            // 直接开始闪烁，不需要先淡入
            if (enableBlinking)
            {
                StartBlinking();
            }
            else
            {
                // 如果不启用闪烁，则设置为完全不透明
                var c = startMessageText.color;
                c.a = 1f;
                startMessageText.color = c;
            }
        }

        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.lockInput = true;
        }

        SetCursorLock(true);

        if (MRespawner.instance != null)
        {
            GameObject player = MRespawner.instance.player;
            if (player != null)
            {
                playerAnimal = player.GetComponent<MAnimal>();
                if (playerAnimal != null && SitModeID != null)
                {
                    playerAnimal.Mode_Activate(SitModeID, 8);
                    Debug.Log("GameManager: 已请求坐下 (Mode: " + SitModeID.name + ", Ability: 8)");
                }
                else
                {
                    Debug.LogWarning("GameManager: 玩家对象存在，但未找到 MAnimal 组件");
                }

                playerInput = playerAnimal.GetComponent<MInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                    Debug.Log("GameManager: 玩家输入已禁用");
                }
                else
                {
                    Debug.LogWarning("GameManager: 未找到 MInput 组件");
                }
            }
            else
            {
                Debug.LogWarning("GameManager: MRespawner.instance.player 为 null");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: 未找到 MRespawner 实例");
        }
        
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.OnEndingTriggered += HandleEndingTriggered;
        }

        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.SetCameraRotation(initialYaw, initialPitch);
            Debug.Log($"GameManager: 初始相机视角设定为 Yaw: {initialYaw}, Pitch: {initialPitch}");
        }

        InitializeCameraSide();
        Debug.Log("GameManager: 游戏已启动");
    }

    void Update()
    {
        HandleInactivityTimer();

        if (Input.GetKeyDown(KeyCode.X))
        {
            UIManager.Instance.hasGameStarted = true;
            
            // 停止闪烁并从当前亮度淡出
            StopBlinking();
            
            // 只在第一次按X键时播放音效
            if (!hasPlayedStartSFX)
            {
                AudioManager.instance.PlayOneShot(FMODEvents.instance.startUISFX, transform.position);
                hasPlayedStartSFX = true;
                Debug.Log("GameManager: 首次按X键，播放开始音效");
            }
            
            if (startMessageText != null && !hasTextFadedOut)
            {
                // 从当前亮度淡出，使用原来的fade out曲线
                FadeText(startMessageText, false, textFadeDuration);
                hasTextFadedOut = true; // 标记文本已经淡出
            }

            if (playerInput != null)
            {
                playerInput.enabled = true;
                Debug.Log("GameManager: 玩家输入已启用");
            }

            if (cameraFollowTarget != null)
            {
                cameraFollowTarget.lockInput = false;
            }

            FadeOutImage(framefadeDuration);
            FadeOutSecondImage(secondImageFadeDuration);
        }

        if (UIManager.Instance.hasGameStarted)
        {
            bool hasMovementInput = Input.GetKey(KeyCode.W) ||
                                    Input.GetKey(KeyCode.A) ||
                                    Input.GetKey(KeyCode.S) ||
                                    Input.GetKey(KeyCode.D);

            if (hasMovementInput)
            {
                OnPlayerInput();
            }
        }

        UpdateCameraSide();
        HandleBasicControls();
        HandleTestingControls();

#if UNITY_EDITOR
        if (previewCameraRotationInRuntime)
        {
            if (cameraFollowTarget != null)
            {
                if (cameraFollowTarget.Target.Value == null)
                {
                    if (MRespawner.instance != null && MRespawner.instance.player != null)
                    {
                        Transform playerTransform = MRespawner.instance.player.transform;
                        cameraFollowTarget.SetTarget(playerTransform);
                        Debug.Log("GameManager (Debug): 自动重新绑定 Camera Target 到玩家");
                    }
                }
                cameraFollowTarget.SetCameraRotation(initialYaw, initialPitch);
            }
        }
#endif
    }
    
    private void HandleBasicControls()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    private void HandleTestingControls()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.ResetNarrativeSystem();
                Debug.Log("GameManager: 已用T键重置叙事系统进行测试");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.HandleFirstInteraction(true);
                NarrativeManager.Instance.HandleSecondInteraction(true);
                Debug.Log("GameManager: 测试路径 - 第一次Yes，第二次Yes (好结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.HandleFirstInteraction(true);
                NarrativeManager.Instance.HandleSecondInteraction(false);
                Debug.Log("GameManager: 测试路径 - 第一次Yes，第二次No (坏结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.HandleFirstInteraction(false);
                NarrativeManager.Instance.HandleSecondInteraction(true);
                Debug.Log("GameManager: 测试路径 - 第一次No，第二次Yes (最坏结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.HandleFirstInteraction(false);
                NarrativeManager.Instance.HandleSecondInteraction(false);
                Debug.Log("GameManager: 测试路径 - 第一次No，第二次No (最坏结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (NarrativeManager.Instance != null)
            {
                Debug.Log("GameManager: 当前叙事系统状态 -\n" + NarrativeManager.Instance.GetSystemStatus());
            }
        }
    }
    
    private void HandleEndingTriggered(NarrativeManager.EndingType endingType)
    {
        currentGameState = GameState.Ending;
        Debug.Log($"GameManager: 结局已触发 - {endingType}");
    }
    
    public void RestartGame()
    {
        currentGameState = GameState.Playing;
        
        // 重置音效标志和闪烁状态
            hasPlayedStartSFX = false;
    hasTextFadedOut = false; // 重置文本淡出标志
    StopBlinking();
        
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.ResetNarrativeSystem();
        }

        SetCursorLock(true);
        UIManager.Instance.hasGameStarted = false;
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        
        Debug.Log("GameManager: 游戏已重启");
    }
    
    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            currentGameState = GameState.Paused;
            Time.timeScale = 0f;
            SetCursorLock(false);
            StopBlinking(); // 暂停时停止闪烁
            Debug.Log("GameManager: 游戏已暂停");
        }
        else if (currentGameState == GameState.Paused)
        {
            currentGameState = GameState.Playing;
            Time.timeScale = 1f;
            SetCursorLock(true);
            // 恢复时重新开始闪烁（如果游戏还没开始）
            if (!UIManager.Instance.hasGameStarted && enableBlinking)
            {
                StartBlinking();
            }
            Debug.Log("GameManager: 游戏已恢复");
        }
    }
    
    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    
    public void SetSkyboxActive(bool active)
    {
        Debug.Log($"GameManager: Skybox状态已设置为 {active}");
    }
    
    public void SetNPCState(NarrativeManager.NPCState state)
    {
        Debug.Log($"GameManager: NPC状态已设置为 {state}");
    }
    
    private void OnDestroy()
    {
        StopBlinking(); // 销毁时停止闪烁
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.OnEndingTriggered -= HandleEndingTriggered;
        }
    }

    public void FadeText(TextMeshProUGUI text, bool fadeIn, float duration)
    {
        if (textFadeCoroutine != null)
            StopCoroutine(textFadeCoroutine);

        textFadeCoroutine = StartCoroutine(FadeTextCoroutine(text, fadeIn, duration));
    }

    private IEnumerator FadeTextCoroutine(TextMeshProUGUI text, bool fadeIn, float duration)
    {
        float startAlpha = fadeIn ? 0f : 1f;  // fade in从0开始，fade out从1开始
        float targetAlpha = fadeIn ? 1f : 0f;

        // 如果是fade out，先立即设置为1（完全不透明）
        if (!fadeIn)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
        }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float normalized = t / duration;
            float curveT = fadeCurve.Evaluate(normalized);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, curveT);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }

        text.color = new Color(text.color.r, text.color.g, text.color.b, targetAlpha);
    }
    public void FadeOutImage(float customDuration)
    {
        if (imageFadeCoroutine != null)
            StopCoroutine(imageFadeCoroutine);

        imageFadeCoroutine = StartCoroutine(FadeImageCoroutine(false, customDuration));
    }

    private IEnumerator FadeImageCoroutine(bool fadeIn, float duration)
    {
        if (uiFadeImage == null) yield break;

        float startAlpha = uiFadeImage.color.a;
        float targetAlpha = fadeIn ? 1f : 0f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float normalized = t / duration;
            float curveT = fadeCurve.Evaluate(normalized);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, curveT);
            Color newColor = uiFadeImage.color;
            newColor.a = alpha;
            uiFadeImage.color = newColor;
            yield return null;
        }

        Color finalColor = uiFadeImage.color;
        finalColor.a = targetAlpha;
        uiFadeImage.color = finalColor;
    }

    private void HandleInactivityTimer()
    {
        if (Input.anyKey || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            inactivityTimer = 0f;
        }
        else
        {
            inactivityTimer += Time.deltaTime;

            if (inactivityTimer >= inactivityTimeThreshold)
            {
                Debug.Log("GameManager: 超过时间未操作，自动重启场景");
                RestartGame();
            }
        }
    }
    public void FadeOutSecondImage(float customDuration)
{
    if (secondImageFadeCoroutine != null)
        StopCoroutine(secondImageFadeCoroutine);

    secondImageFadeCoroutine = StartCoroutine(FadeSecondImageCoroutine(false, customDuration));
}

private IEnumerator FadeSecondImageCoroutine(bool fadeIn, float duration)
{
    if (secondUIFadeImage == null) yield break;

    float startAlpha = secondUIFadeImage.color.a;
    float targetAlpha = fadeIn ? 1f : 0f;

    for (float t = 0f; t < duration; t += Time.deltaTime)
    {
        float normalized = t / duration;
        float curveT = fadeCurve.Evaluate(normalized);
        float alpha = Mathf.Lerp(startAlpha, targetAlpha, curveT);
        Color newColor = secondUIFadeImage.color;
        newColor.a = alpha;
        secondUIFadeImage.color = newColor;
        yield return null;
    }

    Color finalColor = secondUIFadeImage.color;
    finalColor.a = targetAlpha;
    secondUIFadeImage.color = finalColor;
}

}