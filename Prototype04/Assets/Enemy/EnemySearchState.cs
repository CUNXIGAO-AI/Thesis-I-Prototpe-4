using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySearchState : EnemyBaseState
{
    // Start is called before the first frame update
    // Start is called before the first frame update
public override void EnterState(EnemyStateManager enemy)
{
    Debug.Log("Entered Search State");

    // 初始化目标点索引和方向
    if (enemy.searchTargets.Length > 0)
    {
        enemy.searchTargetIndex = 0; // 重置索引
        Transform firstTarget = enemy.searchTargets[enemy.searchTargetIndex];
        Vector3 directionToTarget = (firstTarget.position - enemy.transform.position).normalized;
        enemy.searchTargetRotation = Quaternion.LookRotation(directionToTarget);
    }
}
    public override void UpdateState(EnemyStateManager enemy)
    {
        // 原有逻辑...
        
        // 对于搜索状态，可能有特定的目标点
        if (enemy.searchTargets.Length > 0 && enemy.searchTargetIndex < enemy.searchTargets.Length)
        {
            Transform target = enemy.searchTargets[enemy.searchTargetIndex];
            Vector3 direction = (target.position - enemy.transform.position).normalized;
            
            // 基础旋转
            Quaternion baseRotation = Quaternion.LookRotation(direction);
            
            // 应用抖动效果
            Quaternion targetRotation = enemy.GetJitteredRotation(baseRotation);
            
            // 应用旋转
            float rotationSpeed = enemy.searchRotationSpeeds.Length > enemy.searchTargetIndex ?
                                  enemy.searchRotationSpeeds[enemy.searchTargetIndex] :
                                  enemy.defaultSearchRotationSpeed;
            
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        enemy.canDecreaseAlertMeter = true;

        // 执行旋转逻辑
        PerformSearchRotation(enemy);
    }

    public override void ExitState(EnemyStateManager enemy)
    {
        // 离开搜索状态时清理逻辑（如果需要）
    }

    private static void PerformSearchRotation(EnemyStateManager enemy)
    {
        if (enemy.searchTargets.Length == 0) return;

        // 获取当前目标点和位置
        Transform target = enemy.searchTargets[enemy.searchTargetIndex];
        Vector3 directionToTarget = (target.position - enemy.transform.position).normalized;

        // 计算目标旋转
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // 获取当前旋转速度
        float rotationSpeed = (enemy.searchRotationSpeeds.Length > enemy.searchTargetIndex) 
                            ? enemy.searchRotationSpeeds[enemy.searchTargetIndex] 
                            : enemy.defaultSearchRotationSpeed;

        // 平滑旋转到目标方向
        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // 检查是否完成旋转
        if (Quaternion.Angle(enemy.transform.rotation, targetRotation) < 0.1f)
        {
            enemy.searchWaitTimer += Time.deltaTime;

            // 如果等待时间完成，切换到下一个目标
            if (enemy.searchWaitTimer >= enemy.searchWaitTimeAtPoint)
            {
                enemy.searchWaitTimer = 0f;
                enemy.searchTargetIndex = (enemy.searchTargetIndex + 1) % enemy.searchTargets.Length;
            }
        }
    }
}

