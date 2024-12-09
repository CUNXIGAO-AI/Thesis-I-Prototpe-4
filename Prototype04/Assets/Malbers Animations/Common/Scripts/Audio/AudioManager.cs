using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;


namespace Audio{
public class AudioManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static AudioManager instance { get; private set; }
    private EventInstance stealthMusic;
    private EventInstance onShotSFX;
    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;

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
        eventInstances = new List<EventInstance>();
        eventEmitters = new List<StudioEventEmitter>();
    }


    public void PlayOneShot (EventReference sound, UnityEngine.Vector3 worldPos) 
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    public void SetEnemyStateParameter(int state)
    {
        if (stealthMusic.isValid())
        {
            stealthMusic.setParameterByName("EnemyState", state);
            Debug.Log($"Enemy State set to: {state}");
        }
    }

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public void InitializeStealthMusic(EventReference musicEvent)
    {
        stealthMusic = CreateEventInstance(musicEvent);
        eventInstances.Add(stealthMusic);
    }
    
    public void UpdateStealthMusic()
    {
        if (stealthMusic.isValid())
        {
            PLAYBACK_STATE playbackState;
            stealthMusic.getPlaybackState(out playbackState);
            
            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                stealthMusic.start();
            }
        }
    }

    public void StopStealthMusic(bool allowFadeOut = true)
    {
        if (stealthMusic.isValid())
        {
            stealthMusic.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    public void InitializeOnShotSFX(EventReference sfxEvent)
    {
        onShotSFX = CreateEventInstance(sfxEvent);
        eventInstances.Add(onShotSFX);
    }

    // 启动 OnShot SFX
    public void PlayOnShotSFX()
    {
        if (onShotSFX.isValid())
        {
            PLAYBACK_STATE playbackState;
            onShotSFX.getPlaybackState(out playbackState);

            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                onShotSFX.start();
            }
        }
        else
        {
            Debug.LogWarning("OnShot SFX is not valid or not initialized.");
        }
    }

    // 停止 OnShot SFX
    public void StopOnShotSFX(bool allowFadeOut = true)
    {
        if (onShotSFX.isValid())
        {
            onShotSFX.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        else
        {
            Debug.LogWarning("OnShot SFX is not valid or not initialized.");
        }
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            if (eventInstance.isValid())
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }

        foreach (StudioEventEmitter eventEmitter in eventEmitters)
        {
            eventEmitter.Stop();  
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

}
}

