using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collisionEvent : MonoBehaviour
{
    // Start is called before the first frame update

    void Start()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.windSFX, this.transform.position);
    }
}
