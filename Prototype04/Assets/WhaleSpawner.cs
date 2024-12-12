using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;

public class WhaleSpawner : MonoBehaviour
{
    // Start is called before the first frame update、
    private bool playerInRange = false; // 玩家是否在触发器范围内

private void Update()
    {
        // 玩家在范围内且按下 X 键，并且功能尚未触发过
        if (playerInRange && Input.GetKeyDown(KeyCode.X))
        {
            //spawn whale
            if (AudioManager.instance != null)
            {
                StartCoroutine(AudioManager.instance.EnablewhaleSFXWithDelay(1f)); //播放鲸鱼音效, 1秒后播放, 时间可调整
            }

            //等whale演出播放完毕后，调用 把下面这行Uncomment掉
            //StartCoroutine(AudioManager.instance.DisablewhaleSFXWithDelay(.5f));

        }
    }

    private void OnTriggerEnter(Collider other)
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
}
