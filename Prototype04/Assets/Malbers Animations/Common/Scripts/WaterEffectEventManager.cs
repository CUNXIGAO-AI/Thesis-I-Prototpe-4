using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WaterEffectEventManager
{
    // 定义水面接触事件
    public delegate void WaterContactHandler(Vector3 contactPoint);
    public static event WaterContactHandler OnWaterContact;

    // 触发水面接触事件的方法
    public static void TriggerWaterContact(Vector3 contactPoint)
    {
        OnWaterContact?.Invoke(contactPoint);
    }
}