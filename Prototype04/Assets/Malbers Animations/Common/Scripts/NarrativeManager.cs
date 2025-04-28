using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrativeManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static NarrativeManager Instance { get; private set; }
    
      // 交互状态追踪
    private bool firstInteractionGift = false;  // 第一次交互是否给了礼物
    private bool secondInteractionGift = false; // 第二次交互是否给了礼物
    private bool firstInteractionCompleted = false; // 第一次交互是否已完成
    private bool secondInteractionCompleted = false; // 第二次交互是否已完成
    
    // Skybox状态
    private bool skyboxActive = false;
    
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
        
        // 如果给了礼物，打开Skybox
        if (gaveGift)
        {
            skyboxActive = true;
            Debug.Log("第一次交互给予礼物: 打开Skybox");
        }
        else
        {
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
        
        Debug.Log($"触发结局: {endingType}, NPC状态: {currentNPCState}, Skybox状态: {skyboxActive}");
        
        // 触发结局事件，让其他系统响应
        if (OnEndingTriggered != null)
        {
            OnEndingTriggered(endingType);
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
}

