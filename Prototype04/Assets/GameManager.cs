using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 隐藏鼠标光标并锁定到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 检测按下 R 键以重新开始游戏
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // 可选：按下 Esc 键解锁鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

            if (Input.GetKeyDown(KeyCode.T))
    {
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.ResetNarrativeSystem();
            Debug.Log("已用T键重置叙事系统进行测试");
        }
    }
    
        // 测试用，按G键直接触发好结局
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (NarrativeManager.Instance != null)
            {
                // 确保至少给了一次礼物
                NarrativeManager.Instance.GiveWaterToGirl();
                NarrativeManager.Instance.TriggerEnding();
                Debug.Log("已用G键触发好结局测试");
            }
        }
        
        // 测试用，按B键直接触发坏结局
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (NarrativeManager.Instance != null)
            {
                // 强制设置坏结局
                NarrativeManager.Instance.SetEnding(NarrativeManager.EndingType.Bad);
                Debug.Log("已用B键触发坏结局测试");
            }
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (NarrativeManager.Instance != null)
            {
                // 强制设置坏结局
                NarrativeManager.Instance.GetGiftCount();
            }
        }
    }

    public void RestartGame()
    {
        if (NarrativeManager.Instance != null)
        {
            NarrativeManager.Instance.ResetNarrativeSystem();
        }


        // 重新加载当前场景
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}