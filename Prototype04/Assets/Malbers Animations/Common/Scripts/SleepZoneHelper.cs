using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Controller;

public class SleepZoneHelper : MonoBehaviour
{
 private string playerTag = "Animal";
    [Header("角色拿取系统")]
    [Tooltip("角色的 MPickUp 组件")]
    public MPickUp pickUpComponent;
    
    private void Start()
    {
        // 订阅 MRespawner 的 OnRespawned 事件
        if (MRespawner.instance != null)
        {
            MRespawner.instance.OnRespawned.AddListener(OnPlayerRespawned);
            // 如果已经有活跃的玩家，立即尝试获取其 MPickUp 组件
            if (MAnimal.MainAnimal != null)
            {
                FindPickUpComponent(MAnimal.MainAnimal.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("无法找到 MRespawner 实例！");
        }
    }
    
    // 当玩家重生时调用的方法
    private void OnPlayerRespawned(GameObject newPlayer)
    {
        // 等待一帧确保所有组件都已完全初始化
        StartCoroutine(FindPickUpNextFrame(newPlayer));
    }
    
    private IEnumerator FindPickUpNextFrame(GameObject player)
    {
        yield return null; // 等待一帧
        FindPickUpComponent(player);
    }
    
    // 递归搜索查找 MPickUp 组件
    private void FindPickUpComponent(GameObject player)
    {
        // 递归搜索
        pickUpComponent = FindComponentInChildren<MPickUp>(player.transform);
        
        if (pickUpComponent != null)
        {
            Debug.Log($"在 {pickUpComponent.gameObject.name} 上找到 MPickUp 组件");
        }
        else
        {
            Debug.LogWarning($"在玩家 {player.name} 及其所有子对象上都找不到 MPickUp 组件");
        }
    }
    
    // 递归搜索子对象中的组件
    private T FindComponentInChildren<T>(Transform parent) where T : Component
    {
        // 检查当前对象
        T component = parent.GetComponent<T>();
        if (component != null)
        {
            return component;
        }
        
        // 遍历所有子对象
        foreach (Transform child in parent)
        {
            // 递归检查子对象
            T childComponent = FindComponentInChildren<T>(child);
            if (childComponent != null)
            {
                return childComponent;
            }
        }
        
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"角色 [{other.name}] 进入了触发区域");

            // 如果引用为空，尝试从触发器对象进行深度搜索
            if (pickUpComponent == null)
            {
                FindPickUpComponent(other.gameObject);
            }
            
            if (pickUpComponent != null)
            {
                if (pickUpComponent.Has_Item)
                {
                    Debug.Log($"角色 [{other.name}] 当前正在携带物品。");
                    // 放下物品
                    DropItem();
                }
                else
                {
                    Debug.Log($"角色 [{other.name}] 当前没有携带物品。");
                }
            }
            else
            {
                Debug.LogWarning("无法找到 pickUpComponent！");
            }
        }
    }
    
    // 放下物品的方法
    public void DropItem()
    {
        if (pickUpComponent != null && pickUpComponent.Has_Item)
        {
            // 调用MPickUp组件的Drop方法
            pickUpComponent.DropItem();
            Debug.Log("物品已放下");
        }
        else
        {
            Debug.Log("没有物品可放下");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"角色 [{other.name}] 退出了触发区域");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        if (MRespawner.instance != null)
        {
            MRespawner.instance.OnRespawned.RemoveListener(OnPlayerRespawned);
        }
    }
}