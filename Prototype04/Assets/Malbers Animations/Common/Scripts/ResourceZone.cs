using UnityEngine;
using System.Collections;
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
   [Header("敌人激活设置")]
    [Tooltip("特效完成后要激活的敌人GameObject")]
    public GameObject enemyToActivate;
    [Tooltip("是否在特效完成后激活敌人")]


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
            waterSFXObject.SetActive(false);
        }
    }

    public void ActivateEnemyManually()
{
    Debug.Log("Activating enemy manually.");
}
    public void NotifyPickupAfterTrigger()
{
    if (hasTriggered && enemyToActivate != null && !enemyToActivate.activeInHierarchy)
    {
        enemyToActivate.SetActive(true);
        Debug.Log($"资源被拾取后敌人激活: {enemyToActivate.name}");
    }
}
    
    private float CalculateMaxEffectDuration()
    {
        float maxDuration = 0f;
        
        if (effectsManager == null) return maxDuration;
        
        // 检查所有光源效果
        foreach (var effect in effectsManager.lightEffects)
        {
            float totalDuration = effect.fadeDelay + effect.fadeDuration;
            maxDuration = Mathf.Max(maxDuration, totalDuration);
        }
        
        // 检查所有水流效果
        foreach (var effect in effectsManager.waterEffects)
        {
            float totalDuration = effect.scaleDelay + effect.scaleDuration;
            maxDuration = Mathf.Max(maxDuration, totalDuration);
        }
        
        // 检查所有镜头光晕效果
        foreach (var effect in effectsManager.lensFlareEffects)
        {
            float totalDuration = effect.fadeDelay + effect.fadeDuration;
            maxDuration = Mathf.Max(maxDuration, totalDuration);
        }
        
        // 检查所有雾效果
        foreach (var effect in effectsManager.fogEffects)
        {
            float totalDuration = effect.fadeDelay + effect.fadeDuration;
            maxDuration = Mathf.Max(maxDuration, totalDuration);
        }
        
        // 检查所有材质渐隐效果
        foreach (var effect in effectsManager.materialFadeEffects)
        {
            maxDuration = Mathf.Max(maxDuration, effect.fadeDuration);
        }
        
        return maxDuration;
    }
    
    public void ResetZone()
    {
        hasTriggered = false;
        
        // 重置时也可以选择隐藏敌人
        if (enemyToActivate != null && enemyToActivate.activeInHierarchy)
        {
            enemyToActivate.SetActive(false);
        }
    }

}
