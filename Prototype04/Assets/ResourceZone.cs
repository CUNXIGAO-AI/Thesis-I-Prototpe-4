using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceZone : MonoBehaviour
{
    // Start is called before the first frame update
    public float resourceValue = 10f;  // 在Inspector中可以为每个zone设置不同的值
    public bool hasTriggered = false; // 确保只触发一次
}
