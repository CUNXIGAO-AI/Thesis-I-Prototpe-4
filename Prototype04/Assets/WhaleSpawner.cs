using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;

public class WhaleSpawner : MonoBehaviour
{
    public GameObject whalePrefab; // 鲸鱼预制体
    public Transform spawnPoint;  // 起始点
    public Transform endPoint;    // 终点
    public float speed = 5f;      // 移动速度

    private bool hasSpawned = false; // 是否已经生成鲸鱼

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Animal"))
        {
            SpawnAndMoveWhale();
            hasSpawned = true;
            
            if (AudioManager.instance != null)
            {
                //StartCoroutine(AudioManager.instance.EnablewhaleSFXWithDelay(2f)); // 播放鲸鱼音效, 2秒后播放, 时间可调整
            }
        }
    }

    private void SpawnAndMoveWhale()
    {
        GameObject whale = Instantiate(whalePrefab, spawnPoint.position, Quaternion.identity);
        StartCoroutine(MoveWhale(whale));
    }

    private System.Collections.IEnumerator MoveWhale(GameObject whale)
    {
        while (whale != null)
        {
            // 计算方向并更新鲸鱼的旋转
            Vector3 direction = (endPoint.position - whale.transform.position).normalized;
            whale.transform.rotation = Quaternion.LookRotation(direction);

            // 移动鲸鱼
            whale.transform.position = Vector3.MoveTowards(whale.transform.position, endPoint.position, speed * Time.deltaTime);

            // 如果鲸鱼到达终点，销毁它
            if (Vector3.Distance(whale.transform.position, endPoint.position) < 0.1f)
            {
                Destroy(whale);
                //StartCoroutine(AudioManager.instance.DisablechainSFXWithDelay(1f)); 
                yield break;
            }

            yield return null;
        }
    }
    
    /*
            if (AudioManager.instance != null)
            {
                StartCoroutine(AudioManager.instance.EnablewhaleSFXWithDelay(1f)); // 播放鲸鱼音效, 1秒后播放, 时间可调整
            }
            */
    // 等鲸鱼演出播放完毕后，可以调用下面的方法关闭音效
    // StartCoroutine(AudioManager.instance.DisablewhaleSFXWithDelay(.5f));
}
