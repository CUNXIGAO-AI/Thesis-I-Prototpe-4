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

        Transform targetWaypoint = enemy.waypoints[currentWaypointIndex];
        Vector3 directionToWaypoint = (targetWaypoint.position - enemy.transform.position).normalized;

        // 计算基础旋转和 jittered 旋转
        Quaternion baseRotation = Quaternion.LookRotation(directionToWaypoint);
        Quaternion jitteredRotation = enemy.GetJitteredRotation(baseRotation);

        float rotationSpeed = (enemy.rotationSpeeds.Length > currentWaypointIndex)
                            ? enemy.rotationSpeeds[currentWaypointIndex]
                            : enemy.defaultRotationSpeed;

        // 应用带 jitter 的旋转
        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            jitteredRotation,
            rotationSpeed * Time.deltaTime
        );

        // ✅ 判断是否已经完成了“真正目标方向”的旋转（用 baseRotation）
        if (Quaternion.Angle(enemy.transform.rotation, baseRotation) < 1.5f) // 适当放宽角度容忍度
        {
            enemy.waitTimer += Time.deltaTime;

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
