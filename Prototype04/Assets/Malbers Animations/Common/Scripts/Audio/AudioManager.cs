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
    private List<EventInstance> eventInstances;

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
    }


    public void PlayOneShot (EventReference sound, UnityEngine.Vector3 worldPos) 
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public void InitializeStealthMusic(EventReference musicEvent)
    {
        stealthMusic = CreateEventInstance(musicEvent);
        eventInstances.Add(stealthMusic);
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
    }

    private void OnDestroy()
    {
        CleanUp();
    }

}
}

