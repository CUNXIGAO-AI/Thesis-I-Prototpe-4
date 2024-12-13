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
    }

    public void RestartGame()
    {
        // 重新加载当前场景
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}