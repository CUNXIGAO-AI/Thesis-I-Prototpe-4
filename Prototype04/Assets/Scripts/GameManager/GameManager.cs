using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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
    private Coroutine imageFadeCoroutine;
        [Header("Auto Restart Settings")]
    public float inactivityTimeThreshold = 10f;  // 默认设置为10秒，Inspector可改
    private float inactivityTimer = 0f;
    [Header("Camera Initial View Settings")]
[Range(-180f, 180f)] public float initialYaw = 0f;
[Range(-89f, 89f)] public float initialPitch = 10f;
[Header("Debug Preview (Runtime Only)")]
public bool previewCameraRotationInRuntime = false;

        

    
    // 游戏状态枚举
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Ending
    }
    
    // 当前游戏状态
    private GameState currentGameState = GameState.Playing;
    
    private void Awake()
    {
        // 单例实现
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
        // 等待一帧，确保场景中的对象已完全加载
        yield return null;
        
        // 重新查找 UI 组件
        RefindUIComponents();
        
        // 重新设置玩家引用
        RefindPlayerReferences();
        
        // 重新初始化游戏状态
        InitializeGameState();
    }
      private void InitializeGameState()
    {
        // 如果游戏还未开始，显示开始文本
        if (!UIManager.Instance.hasGameStarted && startMessageText != null)
        {
            var c = startMessageText.color;
            c.a = 0f;
            startMessageText.color = c;
            FadeText(startMessageText, true, textFadeDuration);
        }
        
        // 重新设置相机锁定
        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.lockInput = true;
        }
        
        // 隐藏鼠标光标并锁定
        SetCursorLock(true);
        
        // 如果游戏还未开始，进入坐下状态
        if (!UIManager.Instance.hasGameStarted && playerAnimal != null && SitModeID != null)
        {
            playerAnimal.Mode_Activate(SitModeID, 8);
            Debug.Log("GameManager: 重新激活坐下模式");
        }
        
        // 根据游戏状态设置输入
        if (playerInput != null)
        {
            playerInput.enabled = UIManager.Instance.hasGameStarted;
            Debug.Log($"GameManager: 玩家输入状态设置为 {UIManager.Instance.hasGameStarted}");
        }
    }
    

       private void RefindUIComponents()
    {
        // 查找开始文本
        if (startMessageText == null)
        {
            GameObject startGameObj = GameObject.Find("Start Game");
            if (startGameObj != null)
            {
                startMessageText = startGameObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("GameManager: 重新找到 StartGame 文本组件");
            }
        }
        
        // 查找淡出图片
        if (uiFadeImage == null)
        {
            GameObject startUIObj = GameObject.Find("Start UI");
            if (startUIObj != null)
            {
                uiFadeImage = startUIObj.GetComponent<UnityEngine.UI.Image>();
                Debug.Log("GameManager: 重新找到 Start UI 图片组件");
            }
        }
        
        // 查找相机控制器
        if (cameraFollowTarget == null)
        {
            cameraFollowTarget = FindObjectOfType<ThirdPersonFollowTarget>();
            Debug.Log("GameManager: 重新找到相机控制器");
        }
    }
       private void RefindPlayerReferences()
    {
        // 等待 MRespawner 初始化
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

        // 动态查找 Camera 控制器
        if (cameraFollowTarget == null)
        {
            cameraFollowTarget = FindObjectOfType<ThirdPersonFollowTarget>();
        }


        if (!UIManager.Instance.hasGameStarted && startMessageText != null)
        {
            var c = startMessageText.color;
            c.a = 0f;
            startMessageText.color = c;

            FadeText(startMessageText, true, textFadeDuration);
        }

        if (cameraFollowTarget != null)
        {
            cameraFollowTarget.lockInput = true; // 锁定相机输入
        }


        // 隐藏鼠标光标并锁定到屏幕中心
        SetCursorLock(true);

                if (MRespawner.instance != null)
        {
            GameObject player = MRespawner.instance.player;
            if (player != null)
            {
                playerAnimal = player.GetComponent<MAnimal>();
                if (playerAnimal != null && SitModeID != null)
                {
                    playerAnimal.Mode_Activate(SitModeID, 8);  // 激活 SitMode 的 Ability 8
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
        
        // 订阅NarrativeManager的结局事件
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.OnEndingTriggered += HandleEndingTriggered;
        }

        if (cameraFollowTarget != null)
{
    cameraFollowTarget.SetCameraRotation(initialYaw, initialPitch);
    Debug.Log($"GameManager: 初始相机视角设定为 Yaw: {initialYaw}, Pitch: {initialPitch}");
}
        
        Debug.Log("GameManager: 游戏已启动");

    }

    void Update()
    {
            HandleInactivityTimer();  // 👈 添加这行


        if (Input.GetKeyDown(KeyCode.X))
        {
            UIManager.Instance.hasGameStarted = true;

            if (startMessageText != null)
            {
                FadeText(startMessageText, false, textFadeDuration);
            }

            // 解锁输入
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
            // 退出当前 Mode（例如坐下）
            // 你也可以改变游戏状态，例如切换到 GameState.Playing
        }

        // 基本控制
        HandleBasicControls();
        
        // 测试功能
        HandleTestingControls();

#if UNITY_EDITOR
    if (previewCameraRotationInRuntime)
    {
        if (cameraFollowTarget != null)
        {
            if (cameraFollowTarget.Target.Value == null)
            {
                // 强制重新绑定玩家为 Target
                if (MRespawner.instance != null && MRespawner.instance.player != null)
                {
                    Transform playerTransform = MRespawner.instance.player.transform;
                    cameraFollowTarget.SetTarget(playerTransform);
                    Debug.Log("GameManager (Debug): 自动重新绑定 Camera Target 到玩家");
                }
            }

            // 持续刷新角度
            cameraFollowTarget.SetCameraRotation(initialYaw, initialPitch);
        }
    }
#endif
    }
    
    // 处理基本控制输入
    private void HandleBasicControls()
    {
        // 检测按下 R 键以重新开始游戏
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // 按下 Esc 键切换鼠标锁定状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    // 处理测试功能输入
    private void HandleTestingControls()
    {
        // 测试用，按T键重置叙事系统
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.ResetNarrativeSystem();
                Debug.Log("GameManager: 已用T键重置叙事系统进行测试");
            }
        }
        
        // 测试用，直接触发不同的交互路径
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (NarrativeManager.Instance != null)
            {
                // 路径: Yes -> Yes (好结局)
                NarrativeManager.Instance.HandleFirstInteraction(true);
                NarrativeManager.Instance.HandleSecondInteraction(true);
                Debug.Log("GameManager: 测试路径 - 第一次Yes，第二次Yes (好结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (NarrativeManager.Instance != null)
            {
                // 路径: Yes -> No (坏结局)
                NarrativeManager.Instance.HandleFirstInteraction(true);
                NarrativeManager.Instance.HandleSecondInteraction(false);
                Debug.Log("GameManager: 测试路径 - 第一次Yes，第二次No (坏结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (NarrativeManager.Instance != null)
            {
                // 路径: No -> Yes (最坏结局)
                NarrativeManager.Instance.HandleFirstInteraction(false);
                NarrativeManager.Instance.HandleSecondInteraction(true);
                Debug.Log("GameManager: 测试路径 - 第一次No，第二次Yes (最坏结局)");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (NarrativeManager.Instance != null)
            {
                // 路径: No -> No (最坏结局)
                NarrativeManager.Instance.HandleFirstInteraction(false);
                NarrativeManager.Instance.HandleSecondInteraction(false);
                Debug.Log("GameManager: 测试路径 - 第一次No，第二次No (最坏结局)");
            }
        }
        
        // 打印当前叙事系统状态
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (NarrativeManager.Instance != null)
            {
                Debug.Log("GameManager: 当前叙事系统状态 -\n" + NarrativeManager.Instance.GetSystemStatus());
            }
        }
    }
    
    // 处理结局触发
    private void HandleEndingTriggered(NarrativeManager.EndingType endingType)
    {
        currentGameState = GameState.Ending;
        
        // 解锁鼠标，让玩家可以点击UI
        //SetCursorLock(false);
        
        // 可以在这里添加结局UI显示等逻辑
        Debug.Log($"GameManager: 结局已触发 - {endingType}");
    }
    
    // 重启游戏
   // 重启游戏（保持原有逻辑）
    public void RestartGame()
    {
        currentGameState = GameState.Playing;
        
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.ResetNarrativeSystem();
        }

        SetCursorLock(true);
        
        // 重置游戏开始状态
        UIManager.Instance.hasGameStarted = false;
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        
        Debug.Log("GameManager: 游戏已重启");
    }
    
    // 切换暂停状态
    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            // 暂停游戏
            currentGameState = GameState.Paused;
            Time.timeScale = 0f;
            SetCursorLock(false);
            Debug.Log("GameManager: 游戏已暂停");
        }
        else if (currentGameState == GameState.Paused)
        {
            // 恢复游戏
            currentGameState = GameState.Playing;
            Time.timeScale = 1f;
            SetCursorLock(true);
            Debug.Log("GameManager: 游戏已恢复");
        }
    }
    
    // 设置鼠标锁定状态
    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    
    // 添加控制Skybox的方法
    public void SetSkyboxActive(bool active)
    {
        // 这里可以添加控制Skybox的代码
        // 例如更改RenderSettings.skybox或激活/停用特定的天空盒游戏对象
        Debug.Log($"GameManager: Skybox状态已设置为 {active}");
    }
    
    // 添加控制NPC状态的方法
    public void SetNPCState(NarrativeManager.NPCState state)
    {
        // 这里可以添加控制NPC状态的代码
        // 例如更改NPC的动画状态、激活/停用特定游戏对象等
        Debug.Log($"GameManager: NPC状态已设置为 {state}");
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
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
    float startAlpha = text.color.a;
    float targetAlpha = fadeIn ? 1f : 0f;

    for (float t = 0f; t < duration; t += Time.deltaTime)
    {
        float normalized = t / duration;
        float alpha = Mathf.Lerp(startAlpha, targetAlpha, normalized);
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
        float alpha = Mathf.Lerp(startAlpha, targetAlpha, normalized);
        Color newColor = uiFadeImage.color;
        newColor.a = alpha;
        uiFadeImage.color = newColor;
        yield return null;
    }

    // 确保最终结果
    Color finalColor = uiFadeImage.color;
    finalColor.a = targetAlpha;
    uiFadeImage.color = finalColor;
}

private void HandleInactivityTimer()
{
    if (Input.anyKey || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
    {
        inactivityTimer = 0f;  // 有操作，重置计时
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

}