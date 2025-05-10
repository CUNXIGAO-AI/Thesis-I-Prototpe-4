using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Audio;

public class FmodSoundTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    public Collider smallTrigger; // 进入播放范围
    public Collider largeTrigger; // 离开停止范围

    private EventInstance chandelierInstance;
    private bool isInsideSmallZone = false;
    private bool isInsideLargeZone = false;

    private void Start()
    {
        chandelierInstance = RuntimeManager.CreateInstance(FMODEvents.instance.chandelierSFX);
        RuntimeManager.AttachInstanceToGameObject(chandelierInstance, transform, GetComponent<Rigidbody>());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Animal")) return;

        if (other == smallTrigger)
        {
            isInsideSmallZone = true;
            chandelierInstance.start();
        }

        if (other == largeTrigger)
        {
            isInsideLargeZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Animal")) return;

        if (other == smallTrigger)
        {
            isInsideSmallZone = false;
        }

        if (other == largeTrigger)
        {
            isInsideLargeZone = false;

            // 只有在两个区域都离开后才停止声音
            if (!isInsideSmallZone && !isInsideLargeZone)
            {
                chandelierInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
    }

    private void OnDestroy()
    {
        chandelierInstance.release();
    }


}
