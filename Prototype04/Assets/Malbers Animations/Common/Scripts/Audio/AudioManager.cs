using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;


namespace Audio{
public class AudioManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static AudioManager instance { get; private set; }
    private EventInstance stealthMusic;
    private EventInstance onShotSFX;
    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance chandelierSFX;
        private bool isChandelierPlaying = false;
        private EventInstance chandelierSFX2;
private bool isChandelierSFX2Playing = false;
private EventInstance waterSFX;
private bool isWaterSFXPlaying = false;




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
        
        // 添加这一行来注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
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
                SceneManager.sceneLoaded -= OnSceneLoaded;
        CleanUp();
    }

        public void InitializeChandelierSFX(EventReference sfxEvent, Vector3 initialPosition = default)
        {
            chandelierSFX = CreateEventInstance(sfxEvent);
            eventInstances.Add(chandelierSFX);
            
            // 立即设置一个初始的3D属性，即使不立即播放
            if (initialPosition == default)
                initialPosition = transform.position;
                
            chandelierSFX.set3DAttributes(RuntimeUtils.To3DAttributes(initialPosition));
        }

        // 开始播放吊灯音效
public void PlayChandelierSFX(Vector3 position)
{
    if (chandelierSFX.isValid())
    {
        Debug.DrawRay(position, Vector3.up * 2f, Color.cyan, 2f); // 可视化播放点
        
        // 无论当前状态如何，都始终设置3D属性
        chandelierSFX.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        
        chandelierSFX.getPlaybackState(out var playbackState);

        // 只有在停止状态才开始播放
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            chandelierSFX.start();
            isChandelierPlaying = true;
            Debug.Log("吊灯音效开始播放");
        }
    }
    else
    {
        Debug.LogWarning("吊灯音效未初始化或无效");
    }
}
        // 停止播放吊灯音效
        public void StopChandelierSFX(bool allowFadeOut = true)
        {
            if (chandelierSFX.isValid() && isChandelierPlaying)
            {
                chandelierSFX.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                isChandelierPlaying = false;
                Debug.Log("吊灯音效停止播放");
            }
        }

        // 检查吊灯音效是否在播放
        public bool IsChandelierPlaying()
        {
            return isChandelierPlaying;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景加载时停止所有音效
            StopAllSounds();
            Debug.Log($"场景 '{scene.name}' 已加载，所有音效已停止");
        }
     public void StopAllSounds(bool allowFadeOut = true)
        {
            // 停止吊灯音效
            if (chandelierSFX.isValid() && isChandelierPlaying)
            {
                chandelierSFX.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                isChandelierPlaying = false;
            }

            if (chandelierSFX2.isValid() && isChandelierSFX2Playing)
            {
                chandelierSFX2.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                isChandelierSFX2Playing = false;
            }

            // 停止其他音效
            if (stealthMusic.isValid())
            {
                stealthMusic.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            }

            if (onShotSFX.isValid())
            {
                onShotSFX.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
            

            // 停止所有其他的事件实例
            foreach (EventInstance eventInstance in eventInstances)
            {
                if (eventInstance.isValid())
                {
                    eventInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                }
            }
            
            // 停止所有事件发射器
            foreach (StudioEventEmitter eventEmitter in eventEmitters)
            {
                if (eventEmitter != null)
                {
                    eventEmitter.Stop();
                }
            }


        }

public void InitializeChandelierSFX2(EventReference sfxEvent, Vector3 initialPosition = default)
{
    chandelierSFX2 = CreateEventInstance(sfxEvent);
    eventInstances.Add(chandelierSFX2);
    
    // 立即设置一个初始的3D属性，即使不立即播放
    if (initialPosition == default)
        initialPosition = transform.position;
        
    chandelierSFX2.set3DAttributes(RuntimeUtils.To3DAttributes(initialPosition));
}

// 播放吊灯音效2
public void PlayChandelierSFX2(Vector3 position)
{
    if (chandelierSFX2.isValid())
    {
        Debug.DrawRay(position, Vector3.up * 2f, Color.yellow, 2f); // 可视化播放点
        
        // 无论当前状态如何，都始终设置3D属性
        chandelierSFX2.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        
        chandelierSFX2.getPlaybackState(out var playbackState);

        // 只有在停止状态才开始播放
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            chandelierSFX2.start();
            isChandelierSFX2Playing = true;
            Debug.Log("吊灯音效2开始播放");
        }
    }
    else
    {
        Debug.LogWarning("吊灯音效2未初始化或无效");
    }
}

// 停止播放吊灯音效2
public void StopChandelierSFX2(bool allowFadeOut = true)
{
    if (chandelierSFX2.isValid() && isChandelierSFX2Playing)
    {
        chandelierSFX2.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        isChandelierSFX2Playing = false;
        Debug.Log("吊灯音效2停止播放");
    }
}

public void InitializeWaterSFX(EventReference sfxEvent, Vector3 position)
{
    waterSFX = CreateEventInstance(sfxEvent);
    waterSFX.set3DAttributes(RuntimeUtils.To3DAttributes(position));
    eventInstances.Add(waterSFX);
}

public void PlayWaterSFX(Vector3 position)
{
    if (waterSFX.isValid())
    {
        waterSFX.set3DAttributes(RuntimeUtils.To3DAttributes(position));

        PLAYBACK_STATE playbackState;
        waterSFX.getPlaybackState(out playbackState);
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            waterSFX.start();
            isWaterSFXPlaying = true;
        }
    }
}

public void StopWaterSFX(bool allowFadeOut = true)
{
    if (waterSFX.isValid() && isWaterSFXPlaying)
    {
        waterSFX.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        waterSFX.release();
        isWaterSFXPlaying = false;
    }
}

// 检查吊灯音效2是否在播放
public bool IsChandelierSFX2Playing()
{
    return isChandelierSFX2Playing;
}

public void PlayChainSFX(Vector3 position)
{
    RuntimeManager.PlayOneShot(FMODEvents.instance.chainSFX, position);
}

public void PlayPickupSFX(Vector3 position)
{
    RuntimeManager.PlayOneShot(FMODEvents.instance.pickupSFX, position);

}

public void PlayDropSFX(Vector3 position)
{
    RuntimeManager.PlayOneShot(FMODEvents.instance.dropSFX, position);
}
}
}


