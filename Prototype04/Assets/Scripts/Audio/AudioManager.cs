using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using System.Numerics;

public class AudioManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static AudioManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayOneShot (EventReference sound, UnityEngine.Vector3 worldPos) 
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
}
