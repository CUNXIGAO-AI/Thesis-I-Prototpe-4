using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Audio;
using FMOD.Studio;

public class FmodSoundTrigger : MonoBehaviour
{    [Header("触发目标")]
    [Header("触发目标")]
    [SerializeField] private string targetTag = "Animal";

    [Header("触发区域")]
    [SerializeField] private GameObject smallTriggerObj; // 小范围（进入播放）
    [SerializeField] private GameObject largeTriggerObj; // 大范围（退出停止）
    
    [SerializeField] private enum ChandelierSoundType { ChandelierSFX, ChandelierSFX2 }
    [SerializeField] private ChandelierSoundType soundType = ChandelierSoundType.ChandelierSFX;
    [Tooltip("如果启用，将同时停止两种吊灯音效")]
    [SerializeField] private bool stopBothSounds = true;

    private bool isInsideSmall = false;
    private bool isInsideLarge = false;

    private void Start()
    {
        // 初始化两种声音事件
        AudioManager.instance.InitializeChandelierSFX(FMODEvents.instance.chandelierSFX, transform.position);
        AudioManager.instance.InitializeChandelierSFX2(FMODEvents.instance.chandelierSFX2, transform.position);
        
        // 添加触发器监听脚本
        smallTriggerObj.AddComponent<TriggerListener>().Initialize(this, targetTag, true);
        largeTriggerObj.AddComponent<TriggerListener>().Initialize(this, targetTag, false);
    }
    
    // 由小触发区域调用
    public void OnEnterSmallZone()
    {
        isInsideSmall = true;
        
        // 根据选择播放不同的音效
        if (soundType == ChandelierSoundType.ChandelierSFX)
        {
            AudioManager.instance.PlayChandelierSFX(transform.position);
            Debug.Log("进入小范围，播放吊灯音效1");
        }
        else
        {
            AudioManager.instance.PlayChandelierSFX2(transform.position);
            Debug.Log("进入小范围，播放吊灯音效2");
        }
    }

    // 由小触发区域调用
    public void OnExitSmallZone()
    {
        isInsideSmall = false;
        Debug.Log("离开小范围");
    }

    // 由大触发区域调用
    public void OnEnterLargeZone()
    {
        isInsideLarge = true;
        Debug.Log("进入大范围");
    }

    // 由大触发区域调用
    public void OnExitLargeZone()
    {
        isInsideLarge = false;
        
        // 只有当同时不在小范围和大范围内时，才停止声音
        if (!isInsideSmall && !isInsideLarge)
        {
            if (stopBothSounds)
            {
                // 停止两种吊灯音效
                AudioManager.instance.StopChandelierSFX(true);
                AudioManager.instance.StopChandelierSFX2(true);
                Debug.Log("离开大范围，停止所有吊灯音效");
            }
            else
            {
                // 根据当前选择只停止相应的音效
                if (soundType == ChandelierSoundType.ChandelierSFX)
                {
                    AudioManager.instance.StopChandelierSFX(true);
                    Debug.Log("离开大范围，停止吊灯音效1");
                }
                else
                {
                    AudioManager.instance.StopChandelierSFX2(true);
                    Debug.Log("离开大范围，停止吊灯音效2");
                }
            }
        }
    }

    // 触发器监听器
    private class TriggerListener : MonoBehaviour
    {
        private FmodSoundTrigger manager;
        private string targetTag;
        private bool isSmallTrigger;

        public void Initialize(FmodSoundTrigger manager, string targetTag, bool isSmallTrigger)
        {
            this.manager = manager;
            this.targetTag = targetTag;
            this.isSmallTrigger = isSmallTrigger;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                if (isSmallTrigger)
                    manager.OnEnterSmallZone();
                else
                    manager.OnEnterLargeZone();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                if (isSmallTrigger)
                    manager.OnExitSmallZone();
                else
                    manager.OnExitLargeZone();
            }
        }
    }
}