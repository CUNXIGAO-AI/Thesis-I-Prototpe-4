using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceHandler : MonoBehaviour
{
    private ResourceManager resourceManager;

    private void Start()
    {
        resourceManager = GetComponentInChildren<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogError("找不到ResourceManager!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ResourceZone zone = other.GetComponent<ResourceZone>();
        if (zone != null && !zone.hasTriggered)
        {
            zone.hasTriggered = true;
            resourceManager.AddResource(zone.resourceValue);
             Debug.Log("Resource added: " + zone.resourceValue);
        }
    }
}