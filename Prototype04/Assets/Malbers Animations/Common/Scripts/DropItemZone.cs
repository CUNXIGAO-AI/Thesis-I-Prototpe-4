using UnityEngine;
using MalbersAnimations.Controller;
public class DropItemZone : Zone
{
    [Tooltip("是否在激活区域之前强制放下物品")]
    public bool dropBeforeActivateZone = true;
    
    // 重写Zone的ActivateZone方法
    public override bool ActivateZone(MAnimal animal)
    {
        // 如果启用了掉落前置条件，检查动物是否有持有物品
        if (dropBeforeActivateZone && animal != null)
        {
            // 递归查找MPickUp组件
            MPickUp pickUp = FindComponentInChildren<MPickUp>(animal.transform);
            
            // 如果找到了MPickUp组件并且有物品，则放下
            if (pickUp != null && pickUp.Has_Item)
            {
                Debug.Log($"角色 [{animal.name}] 放下物品后激活区域");
                pickUp.DropItem();
            }
        }
        
        // 调用基类的ActivateZone方法继续原有流程
        return base.ActivateZone(animal);
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
}
