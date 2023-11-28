using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.BossRoom.Infrastructure;

public class FoodSpawner : MonoBehaviour
{
   [SerializeField] private GameObject prefab;


private void Start()
{
    NetworkManager.Singleton.OnServerStarted += SpawnFoodStart;
}

private void SpawnFoodStart()
{
    NetworkManager.Singleton.OnServerStarted -= SpawnFoodStart;
    NetworkObjectPool.Singleton.Awake();
    for (int i = 0; i < 30; ++i)
    {
        SpawnFood();
    }
    StartCoroutine(SpawnOverTime());
}

private void SpawnFood()
{
    NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, 
    GetRandomPositionOnMap(), Quaternion.identity);
    obj.GetComponent<Food>().prefab = prefab;
    obj.Spawn(destroyWithScene:true);
    Debug.Log("SpawningFood");
}

private Vector3 GetRandomPositionOnMap()
{
    return new Vector3(x:Random.Range(-9f, 9f), y:Random.Range(-5f, 5f), z:0f);
}
private IEnumerator SpawnOverTime()
{
    while(NetworkManager.Singleton.ConnectedClients.Count > 0)
    {
        yield return new WaitForSeconds(0.4f);
        SpawnFood();
    }
}
}