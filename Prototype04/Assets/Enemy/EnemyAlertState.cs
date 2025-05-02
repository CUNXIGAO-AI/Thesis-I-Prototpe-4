using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAlertState : EnemyBaseState
{
    // Start is called before the first frame update
public override void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("Entered Alert State");
    }
    
    public override void UpdateState(EnemyStateManager enemy)
    {
        // 使用敌人当前的前方向作为基础旋转
        Quaternion baseRotation = Quaternion.LookRotation(enemy.transform.forward);
        
        // 应用抖动效果
        Quaternion targetRotation = enemy.GetJitteredRotation(baseRotation);
        
        // 应用旋转
        float rotationSpeed = 5f;
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
    
    public override void ExitState(EnemyStateManager enemy)
    {
        // 离开 AlertState 时的清理逻辑（如果需要）
    }
}
