using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using JetBrains.Annotations;

public class PlayerLength : NetworkBehaviour
{
    [SerializeField] private GameObject tailPrefab;
   public NetworkVariable<ushort> length = new(value:1, NetworkVariableReadPermission.Everyone, 
   NetworkVariableWritePermission.Server);

   private List<GameObject> _tails;
   private Transform _lastTail;
   private Collider2D _collider2D;


[CanBeNull] public static event System.Action<ushort> ChangedLengthEvent;

public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();
    _tails = new List<GameObject>();
    _lastTail = transform;
    _collider2D = GetComponent<Collider2D>();
    if(!IsServer) length.OnValueChanged += LengthChangedEvent;
}

public override void OnNetworkDespawn()
{
    base.OnNetworkDespawn();
    DestroyTails();
}

private void DestroyTails()
{
    while(_tails.Count != 0)
    {
        GameObject tail = _tails[0];
        _tails.RemoveAt(index:0);
        Destroy(tail);
    }
}

public void AddLength()
{
    length.Value += 1;
    LengthChanged();
}

private void LengthChanged()
{
    Debug.Log(message:"LengthChanged Callback");
    InsantiateTail();

    if(!IsOwner) return;
    ChangedLengthEvent?.Invoke(length.Value);
    ClientMusicPlayer.Instance.PlayNomAudioClip();
}

private void LengthChangedEvent(ushort previousValue, ushort newValue)
{
    Debug.Log(message:"LengthChanged Callback");
    LengthChanged();
}

private void InsantiateTail()
{
    GameObject tailGameObject = Instantiate(tailPrefab, transform.position, Quaternion.identity);
    tailGameObject.GetComponent<SpriteRenderer>().sortingOrder = -length.Value;
    if(tailGameObject.TryGetComponent(out Tail tail))
    {
        tail.networkedOwner = transform;
        tail.followTransform = _lastTail;
        _lastTail = tailGameObject.transform;
        Physics2D.IgnoreCollision(tailGameObject.GetComponent<Collider2D>() , _collider2D);
    }
    _tails.Add(tailGameObject);
}
}