using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODListenerUpdater : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform targetToFollow;
    private StudioListener listener;

    private void Awake()
    {
        listener = GetComponent<StudioListener>();
    }

    public void SetAttenuationTarget(GameObject player)
    {
        if (player == null || listener == null) return;

        targetToFollow = player.transform;
        listener.attenuationObject = player;
    }
}
