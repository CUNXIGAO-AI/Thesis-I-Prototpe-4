using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatState : EnemyBaseState
{
    private float timeWithoutDetection = 0f; // 用于计时未检测到物品的时间
    private const float detectionTimeout = 5f; // 超时阈值（秒）
public override void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("Entered Combat State");
        enemy.BroadcastCombat();
        timeWithoutDetection = 0f;
        enemy.playerLost = false;
    }

    public override void UpdateState(EnemyStateManager enemy)
    {
        if (enemy.item != null)
        {
            // 计算目标方向
            Vector3 directionToItem = (enemy.item.position - enemy.transform.position).normalized;
            Quaternion baseRotation = Quaternion.LookRotation(directionToItem);
            
            // 应用抖动效果
            Quaternion targetRotation = enemy.GetJitteredRotation(baseRotation);
            
            // 应用旋转
            float rotationSpeed = 5f;
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 检测物品状态
        if (!enemy.DetectItem())
        {
            timeWithoutDetection += Time.deltaTime;

            if (timeWithoutDetection >= detectionTimeout)
            {
                Debug.Log("Player Lost");
                enemy.playerLost = true;
                timeWithoutDetection = 0f;
            }
        }
        else
        {
            timeWithoutDetection = 0f;
            enemy.playerLost = false;
        }
    }

    public override void ExitState(EnemyStateManager enemy)
    {
        // 离开 CombatState 时的清理逻辑（如果需要）
    }
}

