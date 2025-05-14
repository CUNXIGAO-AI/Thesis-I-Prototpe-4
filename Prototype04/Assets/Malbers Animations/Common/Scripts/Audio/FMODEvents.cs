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
    [field: Header("OnWaterGround_sfx")]
    [field: SerializeField] public EventReference onWaterGroundSFX { get; private set; }
    [field: Header("Chandelier_sfx")]
    [field: SerializeField] public EventReference chandelierSFX { get; private set; }
        [field: Header("Chandelier_sfx")]
    [field: SerializeField] public EventReference chandelierSFX2 { get; private set; }

    [field: Header("pickup_sfx")]
    [field: SerializeField] public EventReference pickupSFX { get; private set; }
    [field: Header("drop_sfx")]
    [field: SerializeField] public EventReference dropSFX { get; private set; }
    [field: Header("water_sfx")]
    [field: SerializeField] public EventReference waterSFX { get; private set; }
    [field: Header("interaction_sfx")]
    [field: SerializeField] public EventReference interactionSFX { get; private set; }
    [field: Header("death_sfx")]
    [field: SerializeField] public EventReference deathSFX { get; private set; }
    [field: Header("startui_sfx")]
    [field: SerializeField] public EventReference startUISFX { get; private set; }
    [field: Header("save_sfx")]
    [field: SerializeField] public EventReference saveSFX { get; private set; }
    [field: Header("goodending_sfx")]
    [field: SerializeField] public EventReference goodEndingSFX { get; private set; }
    [field: Header("badending_sfx")]
    [field: SerializeField] public EventReference badEndingSFX { get; private set; }
    

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
