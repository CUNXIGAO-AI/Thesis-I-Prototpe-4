using UnityEngine;

public class ResourceZone : MonoBehaviour
{
    // Start is called before the first frame update
[Header("资源设置")]
    public float resourceValue = 10f;
    public bool hasTriggered = false;
    
    [Header("放置设置")]
    public Transform bottleSnapPoint;
    public float freezeDuration = 6.0f; // 物品放置后多久可以拾取
    
    [Header("效果管理")]
    public ResourceEffectsManager effectsManager;
    
    public void TriggerEffects(float snapDuration)
    {
        if (effectsManager != null)
        {
            effectsManager.TriggerAllEffects(snapDuration);
        }
    }
    
    public void ResetZone()
    {
        hasTriggered = false;
    }

}
