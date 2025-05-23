﻿using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections;
using Audio;

namespace MalbersAnimations.Controller
{
    /// <summary>Use this Script's Transform as the Respawn Point</summary>
    [AddComponentMenu("Malbers/Animal Controller/Respawner")]
    public class MRespawner : MonoBehaviour
    {
        public static MRespawner instance;
        #region Respawn
        [Tooltip("Animal Prefab to Swpawn"), FormerlySerializedAs("playerPrefab")]
        public GameObject player;
public Vector3 playerRotationEuler = Vector3.zero;

        [Header("瓶子控制")]
[Tooltip("玩家是否已经在游戏中获得过瓶子，一旦设置为true将持续有效")]
public bool hasObtainedBottleOnce = false;

[Tooltip("资源管理器引用")]
public ResourceManager resourceManager;

        //[ContextMenuItem("Set Default", "SetDefaultRespawnPoint")]
        //public Vector3Reference RespawnPoint;
        public StateID RespawnState;
        public FloatReference RespawnTime = new(4f);
        [Tooltip("If True: it will destroy the MainPlayer GameObject and Respawn a new One")]
        public BoolReference DestroyAfterRespawn = new(true);
        [Tooltip("The Respawner will be kept between scenes")]
        public BoolReference m_DontDestroyOnLoad = new(true);

        [Tooltip("Restart Scene After Death")]
        public BoolReference RestartScene = new();

        /// <summary>Active Player Animal GameObject</summary>
        private GameObject InstantiatedPlayer;
        /// <summary>Active Player Animal</summary>
        private MAnimal activeAnimal;
        /// <summary>Old Player Animal GameObject</summary>
        private GameObject oldPlayer;
        #endregion

        [FormerlySerializedAs("OnRestartGame")]
        public GameObjectEvent OnRespawned = new();
        private GameObject originalPrefab;
        public Pickable assignedPickableItem;
        Vector3 lastItemPosition = Vector3.zero;  // 记录物品的最后位置
        [Header("死亡黑屏设置")]
[SerializeField] 
private Image uiBackgroundImage; // 黑屏UI背景

[SerializeField]
private float fadeInDuration = 0.5f;

[SerializeField]
private float fadeOutDuration = 0.5f;

[SerializeField]
[Range(0f, 1f)]
private float maxAlpha = 1f;

[Header("重生时间控制")]
[Tooltip("死亡后到黑屏开始的延迟")]
public float deathToFadeDelay = 0.3f;

[Tooltip("黑屏完全显示后的持续时间")]
public float blackScreenDuration = 1.0f;

[Tooltip("重生后到黑屏开始淡出的延迟")]
public float respawnToFadeOutDelay = 0.5f;
private Vector3 cachedDeathPosition = Vector3.zero;



    private void Start()
    {
    // 初始化UI背景
    if (uiBackgroundImage != null)
    {
        // 确保UI背景一开始完全透明
        Color bgColor = uiBackgroundImage.color;
        bgColor.a = 0f;
        uiBackgroundImage.color = bgColor;
    }

    // 其他初始化代码...
    }
    private bool Respawned;

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        // 确保在场景加载完成后重置 Respawned 标志
        Respawned = false;
        FindMainAnimal();
    }

    public virtual void SetPlayer(GameObject go) => player = go;

    void OnEnable()
    {
        if (!isActiveAndEnabled) return;

        if (instance == null)
        {
            instance = this;
            transform.parent = null;
            if (m_DontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            gameObject.name = gameObject.name + " Instance";
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
            FindMainAnimal();
        }
        else
        {
            Destroy(gameObject); //Destroy This GO since is already a Spawner in the scene
        }
    }


    private void OnDisable()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;

            if (activeAnimal != null)
                activeAnimal.OnStateChange.RemoveListener(OnCharacterDead);  //Listen to the Animal changes of states
        }
    }

    public void ResetScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
        Respawned = false;
    }

    public void ResetRespawner(GameObject newPlayer)
    {
        Respawned = false;

        if (activeAnimal != null)
            activeAnimal.OnStateChange.RemoveListener(OnCharacterDead);  //Listen to the Animal changes of states

        SetPlayer(newPlayer);

        if (player == null)
        {
            activeAnimal = MAnimal.MainAnimal;
            if (activeAnimal) player = activeAnimal.gameObject;
        }

        if (player != null)
        {
            if (player.IsPrefab())
            {
                InstantiateNewPlayer();
            }
            else
            {
                if (player.TryGetComponent(out activeAnimal))
                {
                    //Debug.Log("activeAnimal = " + activeAnimal);

                    activeAnimal.OnStateChange.AddListener(OnCharacterDead);        //Listen to the Animal changes of states
                    activeAnimal.OverrideStartState = RespawnState;
                    activeAnimal.SetMainPlayer();
                    Respawned = true;
                }
            }
        }
    }

        /// <summary>Finds the Main Animal used as Player on the Active Scene</summary>
    public virtual void FindMainAnimal()
    {
       if (Respawned) return;

        // 如果player是预制体，保存原始预制体引用
        if (player != null && player.IsPrefab())
        {
            originalPrefab = player;
        }

        activeAnimal = null;
        InstantiatedPlayer = null;
        oldPlayer = null;

        activeAnimal = MAnimal.MainAnimal;
        
        if (activeAnimal != null && activeAnimal.gameObject != null)
        {
            player = activeAnimal.gameObject;
            SceneAnimal();
        }
        else if (originalPrefab != null)
        {
            InstantiateNewPlayer();
        }

        if (activeAnimal != null)
        {
            var DeathState = activeAnimal.State_Get<Death>();
            if (DeathState)
            {
                DeathState.disableAnimal = false;
                DeathState.DisableAllComponents = false;
                DeathState.DisableInternalColliders = false;
                DeathState.DisableMainCollider = false;
            }
        }
    }

            //else
            //{
            //    Debug.LogWarning("[Respawner Removed]. There's no Character assigned", this);
            //    Destroy(gameObject); //Destroy This GO since is already a Spawner in the scene
            //}


    private void SceneAnimal()
{
    if (activeAnimal == null || activeAnimal.gameObject == null)
        return;

    InstantiatedPlayer = activeAnimal.gameObject;
    
    // 确保在添加监听器之前移除已有的监听器
    activeAnimal.OnStateChange.RemoveListener(OnCharacterDead);
    activeAnimal.OnStateChange.AddListener(OnCharacterDead);
    
    // 设置位置和旋转
    activeAnimal.Teleport_Internal(transform.position);
    
    // 使用 playerRotationEuler 而不是 transform.rotation
    Quaternion rotation = Quaternion.Euler(playerRotationEuler);
    activeAnimal.transform.rotation = rotation;
    
    activeAnimal.OverrideStartState = RespawnState;
    activeAnimal.InputSource?.Enable(true);
    if (activeAnimal.MainCollider) activeAnimal.MainCollider.enabled = true;
    activeAnimal.SetMainPlayer();
    Respawned = true;
}

        /// <summary>Listen to the Animal States</summary>
   public void OnCharacterDead(int StateID)
{
    if (activeAnimal != null)
{
    cachedDeathPosition = activeAnimal.transform.position;
}

    if (!Respawned || StateID != StateEnum.Death) return;

    oldPlayer = activeAnimal.gameObject;
    activeAnimal.OnStateChange.RemoveListener(OnCharacterDead);

    if (UIManager.Instance != null)
    {
        UIManager.Instance.HandlePlayerDeath();
    }

    // 如果玩家曾经获得过瓶子，就处理瓶子
    if (hasObtainedBottleOnce && assignedPickableItem != null)
    {
        lastItemPosition = assignedPickableItem.transform.position;
        assignedPickableItem.Drop(); 
    }
    
    StartCoroutine(CoordinatedRespawnSequence());
}

    void DestroyDeathPlayer()
    {
        if (oldPlayer != null)
        {
            // 在销毁之前移除所有监听器
            if (oldPlayer.TryGetComponent(out MAnimal animal))
            {
                animal.OnStateChange.RemoveListener(OnCharacterDead);
            }
            Destroy(oldPlayer);
            oldPlayer = null; // 清除引用
        }
    }

    void InstantiateNewPlayer()
{
    if (UIManager.Instance != null)
    {
        UIManager.Instance.HandlePlayerRespawn();
    }
    
    if (originalPrefab == null)
    {
        Debug.LogError("Cannot instantiate: Missing prefab reference");
        return;
    }

    try 
    {
        // 使用 playerRotationEuler 而不是 transform.rotation
        Quaternion rotation = Quaternion.Euler(playerRotationEuler);
        InstantiatedPlayer = Instantiate(originalPrefab, transform.position, rotation);
        
        if (InstantiatedPlayer == null)
        {
            Debug.LogError("Failed to instantiate player");
            return;
        }

        activeAnimal = InstantiatedPlayer.GetComponent<MAnimal>();
        if (activeAnimal == null)
        {
            Debug.LogError("Instantiated player does not have MAnimal component");
            Destroy(InstantiatedPlayer);
            return;
        }
        
        // 在设置其他属性之前，确保旋转被正确应用
        // 使用 Teleport_Internal 来确保位置和旋转都被正确设置
        activeAnimal.Teleport_Internal(transform.position);
        // 再次设置旋转，确保不被覆盖
        activeAnimal.transform.rotation = rotation;
        
        activeAnimal.OverrideStartState = RespawnState;
        activeAnimal.OnStateChange.AddListener(OnCharacterDead);
        OnRespawned.Invoke(InstantiatedPlayer);
        activeAnimal.SetMainPlayer();
        Respawned = true;

        var animalStats = InstantiatedPlayer.GetComponent<Stats>();
        if (animalStats != null)
        {
            var healthStat = animalStats.Stat_Get("Health");
            if (healthStat != null)
            {
                healthStat.Value = healthStat.MaxValue;
            }
        }

        var listenerUpdater = FindObjectOfType<FMODListenerUpdater>();
        if (listenerUpdater != null)
        {
            listenerUpdater.SetAttenuationTarget(InstantiatedPlayer);
            Debug.Log("FMODListenerUpdater found and set to new player");
        }
        else
        {
            Debug.LogWarning("FMODListenerUpdater 未找到，请确认它挂在摄像机对象上");
        }

        // 如果玩家曾经获得过瓶子，就生成瓶子
        if (hasObtainedBottleOnce && assignedPickableItem != null)
        {
            assignedPickableItem.gameObject.SetActive(true);
            assignedPickableItem.transform.position = transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Debug.Log("Item moved to respawn point - player has obtained bottle before");
        }
        else if (assignedPickableItem != null)
        {
            Debug.Log("Bottle hidden - player has not obtained bottle yet");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error during player instantiation: {e.Message}");
    }
}

    private IEnumerator FadeUIBackground(bool fadeIn)
{
    if (uiBackgroundImage == null) yield break;
    
    float startAlpha = uiBackgroundImage.color.a;
    float targetAlpha = fadeIn ? maxAlpha : 0f;
    float duration = fadeIn ? fadeInDuration : fadeOutDuration;
    
    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
        Color bgColor = uiBackgroundImage.color;
        bgColor.a = newAlpha;
        uiBackgroundImage.color = bgColor;
        yield return null;
    }
    
    Color finalColor = uiBackgroundImage.color;
    finalColor.a = targetAlpha;
    uiBackgroundImage.color = finalColor;
}

private IEnumerator CoordinatedRespawnSequence()
{
    // 先等待死亡到黑屏的延迟
    yield return new WaitForSeconds(deathToFadeDelay);
    
    
    // 淡入黑屏
    StartCoroutine(FadeUIBackground(true));
        if (AudioManager.instance != null)
{
    AudioManager.instance.PlayOneShot(FMODEvents.instance.deathSFX, cachedDeathPosition);
}

    yield return new WaitForSeconds(fadeInDuration);

    
    // 销毁旧玩家
    if (oldPlayer != null) DestroyDeathPlayer();
    
    // 黑屏持续时间
    yield return new WaitForSeconds(blackScreenDuration);
    
    // 等待剩余的重生时间
    float remainingTime = RespawnTime - fadeInDuration - deathToFadeDelay - blackScreenDuration;
    if (remainingTime > 0)
        yield return new WaitForSeconds(remainingTime);
    
    // 重生新玩家
    activeAnimal = null;
    if (originalPrefab != null && originalPrefab.IsPrefab())
    {
        InstantiateNewPlayer();
        
        // 等待重生后到黑屏淡出的延迟
        yield return new WaitForSeconds(respawnToFadeOutDelay);
        
        // 淡出黑屏
        StartCoroutine(FadeUIBackground(false));
    }
    else
    {
        Debug.LogError("[Respawner] No valid prefab reference for respawn.");
        // 即使出错也尝试淡出黑屏
        StartCoroutine(FadeUIBackground(false));
    }
}

public void MarkBottleObtained()
{
    // 只有当瓶子尚未被标记为获得时，才设置标志
    if (!hasObtainedBottleOnce)
    {
        hasObtainedBottleOnce = true;
        Debug.Log("玩家已首次获得瓶子，后续重生将包含瓶子");
    }
}
}


}
