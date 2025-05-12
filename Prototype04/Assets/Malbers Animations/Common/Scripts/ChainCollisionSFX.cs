using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;

public class ChainCollisionSFX : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Animal"))  // 或者使用 Layer 判断
        {
            Debug.Log("Animal Collision Detected");
            if (collision.relativeVelocity.magnitude > 0.1f)  // 阈值避免太轻微的触发
            {
                AudioManager.instance.PlayChainSFX(transform.position);
            }
        }
    }
}
