using UnityEngine;
using UnityEngine.Events;

public class NarrativeAction : MonoBehaviour
{
    // Start is called before the first frame update
    public enum ActionType
    {
        GiveWaterToGirl,      // 给女孩水
        TriggerEnding,        // 触发结局（基于当前的礼物计数）
        CustomAction          // 自定义动作
    }
    
    // 要执行的动作类型
    public ActionType actionType;
    
    // 可选：自定义动作的UnityEvent
    public UnityEvent customAction;
    
    // 执行叙事动作
    public void ExecuteAction()
    {
        // 首先检查NarrativeManager是否存在
        if (NarrativeManager.Instance == null && actionType != ActionType.CustomAction)
        {
            Debug.LogError("NarrativeManager实例不存在，无法执行叙事动作");
            return;
        }
        
        switch (actionType)
        {
            case ActionType.GiveWaterToGirl:
                Debug.Log("执行动作：给女孩水");
                NarrativeManager.Instance.GiveWaterToGirl();
                break;
                
            case ActionType.TriggerEnding:
                Debug.Log("执行动作：触发结局（基于当前礼物计数）");
                NarrativeManager.Instance.TriggerEnding();
                break;
                
            case ActionType.CustomAction:
                customAction?.Invoke();
                break;
        }
    }
}
