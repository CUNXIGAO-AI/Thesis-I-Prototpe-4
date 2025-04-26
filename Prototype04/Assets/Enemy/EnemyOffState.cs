using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyOffState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("Entered OffState");
        enemy.StartCoroutine(DelayedDimLight(enemy.alertSpotLight));
    }

    public override void UpdateState(EnemyStateManager enemy)
    {
        // 这里无需其他逻辑
    }

    public override void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("Exiting OffState.");
    }

    private IEnumerator DelayedDimLight(Light light)
    {
        // 设置延迟时间（秒）
        float delay = 1.5f; // 您可以根据需要调整这个值
        
        Debug.Log("Waiting for " + delay + " seconds before dimming light...");
        
        // 等待指定的延迟时间
        yield return new WaitForSeconds(delay);
        
        Debug.Log("Delay completed, starting to dim light now.");
        
        // 延迟结束后，启动原来的熄灯协程
        yield return DimLight(light);
    }

    private IEnumerator DimLight(Light light) 
    {
        if (light == null)
        {
            Debug.LogWarning("No SpotLight found. Skipping dimming process.");
            yield break;
        }

        float duration = 3.0f; // 持续时间（秒）
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            light.intensity = Mathf.Lerp(startIntensity, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime; // 累加经过的时间
            yield return null; // 等待下一帧
        }

        light.intensity = 0f; // 确保最终亮度为 0
        Debug.Log("Light is completely dimmed.");
    }
}
