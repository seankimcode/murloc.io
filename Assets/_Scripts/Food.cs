using System;
using Unity.Netcode;
using UnityEngine;
using Unity.BossRoom.Infrastructure;
public class Food : MonoBehaviour
{
    private NetworkObject networkObject; // Reference to NetworkObject
    public GameObject prefab;
    private void Awake()
    {
        // Get the NetworkObject component attached to this GameObject
        networkObject = GetComponent<NetworkObject>();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if(!col.CompareTag("Player")) return;

        if(!NetworkManager.Singleton.IsServer) return;

        if(col.TryGetComponent(out PlayerLength playerLength))
        {
            playerLength.AddLength();
        }
        else if(col.TryGetComponent(out Tail tail))
        {
            tail.networkedOwner.GetComponent<PlayerLength>().AddLength();
        }
        if (networkObject != null)
        {
            networkObject.Despawn();
        }
        NetworkObjectPool.Singleton.ReturnNetworkObject(networkObject, prefab);
    }
}
