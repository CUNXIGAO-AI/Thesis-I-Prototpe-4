using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyBaseState
{
    private int currentWaypointIndex = 0;  // 当前的巡逻点索引
    private NavMeshAgent navAgent;
    // Start is called before the first frame update

    public override void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("Entered Patrol State");
        navAgent = enemy.GetComponent<NavMeshAgent>();
        navAgent.speed = enemy.patrolSpeed;

        if (enemy.rotationPoints.Length > 0)
        {
            enemy.targetRotation = Quaternion.Euler(enemy.rotationPoints[enemy.currentRotationIndex]);
        }
    }

    public override void UpdateState(EnemyStateManager enemy)
    {
        Patrol(enemy);
        PerformRotationAtWaypoint(enemy);
    
    }

    public override void ExitState(EnemyStateManager enemy)
    {
        // 离开巡逻状态时执行的逻辑
    }

    private void PerformRotationAtWaypoint(EnemyStateManager enemy)
    {
        if (enemy.waypoints.Length == 0) return;

        // 获取当前目标点
        Transform targetWaypoint = enemy.waypoints[currentWaypointIndex];

        // 计算目标旋转
        Quaternion targetRotation = Quaternion.LookRotation((targetWaypoint.position - enemy.transform.position).normalized);

        // 获取当前旋转速度
        float rotationSpeed = (enemy.rotationSpeeds.Length > currentWaypointIndex) 
                            ? enemy.rotationSpeeds[currentWaypointIndex] 
                            : enemy.defaultRotationSpeed;

        // 平滑旋转到目标方向
        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // 检查是否完成旋转并开始等待
        if (Quaternion.Angle(enemy.transform.rotation, targetRotation) < 0.1f)
        {
            enemy.waitTimer += Time.deltaTime;

            // 如果等待时间完成，移动到下一个点
            if (enemy.waitTimer >= enemy.waitTimeAtWaypoint)
            {
                enemy.waitTimer = 0f;
                currentWaypointIndex = (currentWaypointIndex + 1) % enemy.waypoints.Length;
            }
        }
    }
    private void Patrol(EnemyStateManager enemy)
    {
        if (enemy.waypoints.Length == 0) return;

        // 获取当前巡逻点
        Transform targetWaypoint = enemy.waypoints[currentWaypointIndex];

        // 检查是否接近目标巡逻点
        if (Vector3.Distance(enemy.transform.position, targetWaypoint.position) <= enemy.waypointReachThreshold)
        {
            // 在到达巡逻点后执行旋转逻辑
            PerformRotationAtWaypoint(enemy);
        }
    }
}
