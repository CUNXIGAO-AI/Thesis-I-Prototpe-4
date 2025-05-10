using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class FMODEvents : MonoBehaviour
{
    // Start is called before the first frame update
    [field: Header("Player_sfx")]
    [field: SerializeField] public EventReference footstepsSFX { get; private set; }

    [field: Header("StealthMusic")]
    [field: SerializeField] public EventReference stealthMusic { get; private set; }
    
    [field: Header("Egg_sfx")]
    [field: SerializeField] public EventReference patrolbassSFX { get; private set; }

    [field: Header("Onshot_sfx")]
    [field: SerializeField] public EventReference onShotSFX { get; private set; }
    
    [field: Header("Chain_sfx")]
    [field: SerializeField] public EventReference chainSFX { get; private set; }
    

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
    }
}
