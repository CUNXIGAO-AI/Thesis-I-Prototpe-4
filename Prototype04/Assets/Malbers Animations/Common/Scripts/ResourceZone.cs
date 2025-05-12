using UnityEngine;
using Audio;

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
    [Header("水源音效控制")]
public GameObject waterSFXObject;  // 拖入带 StudioEventEmitter 的 GameObject

    private void Start()
{
    if (waterSFXObject != null)
    {
        waterSFXObject.SetActive(true);  // 播放水声（必须设置 Play On Enable）
    }
}
    
    public void TriggerEffects(float snapDuration)
    {
        if (effectsManager != null)
        {
            effectsManager.TriggerAllEffects(snapDuration);
        }
        if (waterSFXObject != null)
        {
            waterSFXObject.SetActive(false);  // 停止水声
        }
    }
    
    public void ResetZone()
    {
        hasTriggered = false;
    }

}
