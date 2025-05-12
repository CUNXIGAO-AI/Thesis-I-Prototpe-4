using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyOffState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("Entered OffState");
        enemy.StartCoroutine(FadeOutRoutine(enemy));
        enemy.TurnOffMusic();
    }

    public override void UpdateState(EnemyStateManager enemy)
    {
        // 这里无需其他逻辑
    }

    public override void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("Exiting OffState.");
    }

    private IEnumerator FadeOutRoutine(EnemyStateManager enemy)
    {
        Light light = enemy.alertSpotLight;

        yield return new WaitForSeconds(enemy.fadeOutDelay);
        enemy.TurnOffMusic();

        if (light != null)
        {
            float elapsedTime = 0f;
            float startValue = 1f;

            // 逐渐让 flicker 整体变暗
            while (elapsedTime < enemy.fadeOutDuration)
            {
                float progress = elapsedTime / enemy.fadeOutDuration;
                enemy.flickerFadeMultiplier = Mathf.Lerp(startValue, 0f, progress);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            enemy.flickerFadeMultiplier = 0f;
            light.enabled = false;
        }
    }
}
