using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class EnemyStateParameterController : MonoBehaviour
{
    // Studio Event Emitter 组件
 [SerializeField]
    private StudioEventEmitter eventEmitter;

    // 参数名称
    [SerializeField]
    private string parameterName = "EnemyState";

    private void Start()
    {
        if (eventEmitter == null)
        {
            Debug.LogError("StudioEventEmitter 未设置，请在 Inspector 中拖入对应的组件！");
            return;
        }

        if (eventEmitter.EventReference.IsNull)
        {
            Debug.LogError("StudioEventEmitter 的 EventReference 为空，请检查事件是否已正确分配！");
        }
    }

    public void SetParameterValue(float value)
    {
        if (eventEmitter != null && eventEmitter.EventInstance.isValid())
        {
            eventEmitter.EventInstance.setParameterByName(parameterName, value);
            Debug.Log($"参数 {parameterName} 已设置为: {value}");
        }
        else
        {
            Debug.LogError("EventInstance 无效，请确保事件已正确加载并正在播放！");
        }
    }

    private void OnDestroy()
    {
        if (eventEmitter != null && eventEmitter.EventInstance.isValid())
        {
            eventEmitter.EventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            eventEmitter.EventInstance.release();
        }
    }
}