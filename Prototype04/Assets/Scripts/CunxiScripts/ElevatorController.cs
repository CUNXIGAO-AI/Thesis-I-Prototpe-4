using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;

public class ElevatorController : MonoBehaviour
{
    public Transform elevator; // 电梯对象
    public Transform startPoint; // 起始点
    public Transform endPoint; // 终点
    public float speed = 2f; // 移动速度

    private bool playerInRange = false; // 玩家是否在触发器范围内
    private bool isMoving = false; // 电梯是否正在移动
    private bool isAtEndPoint = false; // 电梯是否在终点
    
    private bool hasPadPressed = false;
    public GameObject player;
    public Transform originalParent; // 保存玩家的原始父对象
    public Transform elevatorParent; // 电梯的父对象

    private void FixedUpdate()
    {
        // 当玩家在范围内按下 X 键时，启动电梯移动到终点
        if (playerInRange && hasPadPressed && !isMoving && !isAtEndPoint)
        {
            StartCoroutine(MoveElevator(endPoint.position)); // 开始移动到终点
            AudioManager.instance.EnableElevatorSFX();
        }

        // 当玩家离开范围且电梯在终点时，启动返回起点
        if (!playerInRange && isAtEndPoint && !isMoving)
        {
            StartCoroutine(MoveElevator(startPoint.position)); // 开始移动到起点
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            playerInRange = true;
            if (player != null)
            {
                player.transform.SetParent(elevatorParent.transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            playerInRange = false;
            if (player != null)
            {
                player.transform.SetParent(originalParent);
            }
        }
    }

    // 协程：平滑移动电梯
    private System.Collections.IEnumerator MoveElevator(Vector3 targetPosition)
    {
        isMoving = true;

        while (Vector3.Distance(elevator.position, targetPosition) > 0.01f)
        {
            // 平滑移动到目标位置
            elevator.position = Vector3.MoveTowards(elevator.position, targetPosition, speed * Time.deltaTime);
            yield return null; // 等待下一帧
        }

        // 确保完全到达目标位置
        elevator.position = targetPosition;
        AudioManager.instance.DisableElevatorSFX();

        // 更新状态
        isMoving = false;
        isAtEndPoint = targetPosition == endPoint.position;
    }
    
    public void PressElevatorPad()
    {
        hasPadPressed = true;
    }
}
