using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class FMODEvents : MonoBehaviour
{
    // Start is called before the first frame update
    [field: Header("playerSFX")]
    [field: SerializeField] public EventReference footstepsSFX { get; private set; }

    [field: Header("WindSFX")]
    [field: SerializeField] public EventReference windSFX { get; private set; }

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
            DontDestroyOnLoad(gameObject);
        }
    }
}
