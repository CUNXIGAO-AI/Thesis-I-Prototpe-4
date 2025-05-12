using UnityEngine;
using UnityEngine.Events;

public class NarrativeAction : MonoBehaviour
{
    // Start is called before the first frame update
    public ResourceEffectsManager resourceEffectsManager;

public enum ActionType
    {
        FirstInteractionYes,   // 第一次交互给礼物
        FirstInteractionNo,    // 第一次交互不给礼物
        SecondInteractionYes,  // 第二次交互给礼物
        SecondInteractionNo,   // 第二次交互不给礼物
        ResetSystem,           // 重置系统
        CustomAction           // 自定义动作
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
            case ActionType.FirstInteractionYes:
                Debug.Log("执行动作：第一次交互-给予礼物");
                NarrativeManager.Instance.HandleFirstInteraction(true);

                if (resourceEffectsManager != null)
                    resourceEffectsManager.TriggerAllEffects(0f); // 可自定义 snap delay
                break;

            
            case ActionType.FirstInteractionNo:
                Debug.Log("执行动作：第一次交互-不给礼物");
                NarrativeManager.Instance.HandleFirstInteraction(false);
                break;
                
            case ActionType.SecondInteractionYes:
                Debug.Log("执行动作：第二次交互-给予礼物");
                NarrativeManager.Instance.HandleSecondInteraction(true);
                break;
                
            case ActionType.SecondInteractionNo:
                Debug.Log("执行动作：第二次交互-不给礼物");
                NarrativeManager.Instance.HandleSecondInteraction(false);
                break;
                
            case ActionType.ResetSystem:
                Debug.Log("执行动作：重置叙事系统");
                NarrativeManager.Instance.ResetNarrativeSystem();
                break;
                
            case ActionType.CustomAction:
                customAction?.Invoke();
                break;
        }
    }
    
}
