using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
        public static GameManager Instance { get; private set; }
    
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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 隐藏鼠标光标并锁定到屏幕中心
        SetCursorLock(true);
        
        // 订阅NarrativeManager的结局事件
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.OnEndingTriggered += HandleEndingTriggered;
        }
        
        Debug.Log("GameManager: 游戏已启动");
    }

    void Update()
    {
        // 基本控制
        HandleBasicControls();
        
        // 测试功能
        HandleTestingControls();
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
    public void RestartGame()
    {
        // 重置游戏状态
        currentGameState = GameState.Playing;
        
        // 重置叙事系统
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.ResetNarrativeSystem();
        }

        // 重新锁定鼠标
        SetCursorLock(true);
        
        // 重新加载当前场景
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
}