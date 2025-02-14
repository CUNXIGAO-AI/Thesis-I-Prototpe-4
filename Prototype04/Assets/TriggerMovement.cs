using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerMovement : MonoBehaviour
{
    // 可以在其他接口调用的时候 就开关这两个component即可 
    // 在inspector中拖拽component
    public MonoBehaviour movingComponent;  
    public MonoBehaviour rotatingComponent; 

    private void Start()
    {
        // 先禁用组件
        if (movingComponent) movingComponent.enabled = false;
        if (rotatingComponent) rotatingComponent.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {       
        if (other.CompareTag("Animal"))
        {
            // 碰撞时，启用组件
            // 可以在其他接口调用的时候 就开关这两个component即可 
            if (movingComponent) movingComponent.enabled = true;
            if (rotatingComponent) rotatingComponent.enabled = true;
        }
    }
}
