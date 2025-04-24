using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MalbersAnimations.Controller
{
    /// <summary>Use this Script's Transform as the Respawn Point</summary>
    [AddComponentMenu("Malbers/Animal Controller/Respawner")]
    public class MRespawner : MonoBehaviour
    {
        public static MRespawner instance;
        [SerializeField] private InteractionTrigger interactionTrigger; // 在 Unity Inspector 中指定

        #region Respawn
        [Tooltip("Animal Prefab to Swpawn"), FormerlySerializedAs("playerPrefab")]
        public GameObject player;

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
        
        activeAnimal.Teleport_Internal(transform.position);
        activeAnimal.transform.rotation = transform.rotation;
        activeAnimal.OverrideStartState = RespawnState;
        activeAnimal.InputSource?.Enable(true);
        if (activeAnimal.MainCollider) activeAnimal.MainCollider.enabled = true;
        activeAnimal.SetMainPlayer();
        Respawned = true;
    }

        /// <summary>Listen to the Animal States</summary>
    public void OnCharacterDead(int StateID)
    {
        if (!Respawned || StateID != StateEnum.Death) return;

        oldPlayer = activeAnimal.gameObject;
        activeAnimal.OnStateChange.RemoveListener(OnCharacterDead);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HandlePlayerDeath();
        }


        if (assignedPickableItem != null)
        {
            lastItemPosition = assignedPickableItem.transform.position;  // 记录物品掉落的位置
            assignedPickableItem.Drop(); 
            
        }
        

        this.Delay_Action(RespawnTime, () =>
        {
            if (oldPlayer != null) DestroyDeathPlayer();
            this.Delay_Action(() =>
            {
                activeAnimal = null;
                if (originalPrefab != null && originalPrefab.IsPrefab())
                {
                    InstantiateNewPlayer();
                }
                else
                {
                    Debug.LogError("[Respawner] No valid prefab reference for respawn.");
                }
            });
        });
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
            InstantiatedPlayer = Instantiate(originalPrefab, transform.position, transform.rotation);
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

            // **让物品传送到新玩家的附近**
            if (assignedPickableItem != null)
            {
                assignedPickableItem.transform.position = transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                Debug.Log("Item moved to respawn point");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during player instantiation: {e.Message}");
        }
    }


}

}
