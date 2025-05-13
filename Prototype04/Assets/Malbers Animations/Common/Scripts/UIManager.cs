using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    public bool hasGameStarted = false;
// 单例模式
    public static UIManager Instance { get; private set; }
    
    // 定义事件
    public UnityEvent OnPlayerDeath = new UnityEvent();
    public UnityEvent OnPlayerRespawn = new UnityEvent();
    
    // 所有交互UI的列表
    private List<InteractionTrigger> activeInteractions = new List<InteractionTrigger>();
    
    private void Awake()
    {
        // 单例设置
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
    
    // 注册交互UI
    public void RegisterInteraction(InteractionTrigger interaction)
    {
        if (!activeInteractions.Contains(interaction))
        {
            activeInteractions.Add(interaction);
        }
    }
    
    // 取消注册交互UI
    public void UnregisterInteraction(InteractionTrigger interaction)
    {
        if (activeInteractions.Contains(interaction))
        {
            activeInteractions.Remove(interaction);
        }
    }
    
    // 处理玩家死亡
    public void HandlePlayerDeath()
    {
        // 通知所有注册的交互UI
        foreach (var interaction in activeInteractions)
        {
            if (interaction != null)
            {
                interaction.OnPlayerDeath();
            }
        }
        
        // 触发死亡事件
        OnPlayerDeath.Invoke();
    }
    
    // 处理玩家重生
    public void HandlePlayerRespawn()
    {
        // 通知所有注册的交互UI
        foreach (var interaction in activeInteractions)
        {
            if (interaction != null)
            {
                interaction.ResetDeathState();
            }
        }
        
        // 触发重生事件
        OnPlayerRespawn.Invoke();
    }
}

