using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrativeManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static NarrativeManager Instance { get; private set; }
    
    // 记录给NPC礼物的次数
    private int giftCount = 0;
    
    // 结局是否已经触发
    private bool endingTriggered = false;
    
    // 结局类型枚举
    public enum EndingType
    {
        Good,
        Bad
    }
    
    // 结局触发时的事件委托
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
    
    // 当玩家给NPC礼物时调用
    public void GiveWaterToGirl()
    {
        giftCount++;
        Debug.Log($"给予NPC礼物计数: {giftCount}");
    }
    
    // 判断并触发结局
    public void TriggerEnding()
    {
        // 如果结局已触发，不重复触发
        if (endingTriggered) return;
        
        endingTriggered = true;
        EndingType currentEnding;
        
        // 根据礼物数量决定结局
        if (giftCount >= 1)
        {
            currentEnding = EndingType.Good;
            Debug.Log("触发好结局：你成功帮助了NPC，获得了好结局!");
        }
        else
        {
            currentEnding = EndingType.Bad;
            Debug.Log("触发坏结局：你没有足够帮助NPC，获得了坏结局...");
        }
        
        // 触发结局事件，让其他系统响应
        if (OnEndingTriggered != null)
        {
            OnEndingTriggered(currentEnding);
        }
    }
    
    // 手动设置结局(可用于测试或特殊情况)
    public void SetEnding(EndingType endingType)
    {
        if (endingTriggered) return;
        
        endingTriggered = true;
        Debug.Log($"手动设置结局: {endingType}");
        
        if (OnEndingTriggered != null)
        {
            OnEndingTriggered(endingType);
        }
    }
    
    // 重置叙事系统状态
    public void ResetNarrativeSystem()
    {
        giftCount = 0;
        endingTriggered = false;
        Debug.Log("叙事系统已重置");
    }
    
    // 获取当前礼物计数(用于外部查询)
    public int GetGiftCount()
    {
        Debug.Log ($"当前礼物计数: {giftCount}");
        return giftCount;
    }
    
    // 获取当前结局状态
    public bool IsEndingTriggered()
    {
        return endingTriggered;
    }
}
