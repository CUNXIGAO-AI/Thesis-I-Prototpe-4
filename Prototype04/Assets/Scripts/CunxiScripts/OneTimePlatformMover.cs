using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;

public class OneTimePlatformMover : MonoBehaviour
{
    public Transform platform; // 需要移动的平台
    public float moveDistance = 5f; // 移动的距离（正数向上，负数向下）
    public float speed = 2f; // 平台移动的速度
    private bool playerInRange = false; // 玩家是否在触发器范围内
    private bool hasMoved = false; // 是否已经触发过移动
    public bool isAudioSource = false; // 当前物体是否负责播放音频
    
    private bool hasPadPressed = false; // 是否已经按下了 X 键

    private void Update()
    {
        // 玩家在范围内且按下 X 键，并且功能尚未触发过
        if (playerInRange && hasPadPressed && !hasMoved)
        {
            hasMoved = true; // 标记为已触发
            StartCoroutine(MovePlatform()); // 开始移动平台
        if (isAudioSource && AudioManager.instance != null)
            {
                StartCoroutine(AudioManager.instance.EnableRoomAmbienceWithDelay(1.5f));
                AudioManager.instance.EnablePlatformSFX();
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Animal")) // 检查是否是玩家
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal")) // 检查是否是玩家
        {
            playerInRange = false;
        }
    }

    private System.Collections.IEnumerator MovePlatform()
    {
        Vector3 targetPosition = platform.position + new Vector3(0, moveDistance, 0); // 计算目标位置

        while (Vector3.Distance(platform.position, targetPosition) > 0.01f)
        {
            // 平滑移动平台到目标位置
            platform.position = Vector3.MoveTowards(platform.position, targetPosition, speed * Time.deltaTime);
            yield return null; // 等待下一帧
        }

        // 确保平台完全到达目标位置
        platform.position = targetPosition;
        AudioManager.instance.DisablePlatformSFX();
    }
    
    public void PressPad()
    {
        hasPadPressed = true;
    }

}
