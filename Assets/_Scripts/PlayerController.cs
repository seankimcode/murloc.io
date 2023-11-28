using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 3f;
    
    [CanBeNull] public static event System.Action GameOverEvent;

    private Camera _mainCamera;
    private Vector3 _mouseInput = Vector3.zero;
    private PlayerLength _playerLength;
    private bool _canCollide = true;

    private readonly ulong[] _targetClientsArray = new ulong[1];
 
    private void Initialize()
    {
        _mainCamera = Camera.main;
        _playerLength = GetComponent<PlayerLength>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Update()
   {
        if(!IsOwner || !Application.isFocused) return;
        MovePlayerServer();

   }

   private void MovePlayerServer()
   {
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint((Vector3)_mouseInput);
        mouseWorldCoordinates.z = 0f;
        MovePlayerServerRpc(mouseWorldCoordinates);
   }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 mouseWorldCoordinates)
    {
        transform.position = Vector3.MoveTowards(current:transform.position, 
        target:mouseWorldCoordinates, maxDistanceDelta:Time.deltaTime * speed);

        //rotate
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }
    
    private void MovePlayerClient()
    {
        //movement
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint((Vector3)_mouseInput);
        mouseWorldCoordinates.z = 0f;
        transform.position = Vector3.MoveTowards(current:transform.position, 
        target:mouseWorldCoordinates, maxDistanceDelta:Time.deltaTime * speed);

        //rotate
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DetermineCollisionWinnerServerRpc(PlayerData player1, PlayerData player2)
    {
        if (player1.Length > player2.Length)
        {
            WinInformationServerRpc(winner:player1.Id, loser:player2.Id);
        }
        else
        {
            WinInformationServerRpc(winner:player2.Id, loser:player1.Id);
        }
    }
   

   [ServerRpc]
   private void WinInformationServerRpc(ulong winner, ulong loser)
   {
        _targetClientsArray[0] = winner;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = _targetClientsArray
            }
        };
        AtePlayerClientRpc(clientRpcParams);
        
        _targetClientsArray[0] = loser;
        clientRpcParams.Send.TargetClientIds = _targetClientsArray;
        
        GameOverClientRpc(clientRpcParams);
   }

   [ClientRpc]
   private void AtePlayerClientRpc(ClientRpcParams clientRpcParams = default)
   {
    if(!IsOwner) return;
    Debug.Log(message:"You ate a player!");
   }

   [ClientRpc]
   private void GameOverClientRpc(ClientRpcParams clientRpcParams = default)
   {
    if (!IsOwner) return;
    Debug.Log(message:"You died!");
    GameOverEvent?.Invoke();
    NetworkManager.Singleton.Shutdown();
   }
   
   private IEnumerator CollisionCheckCoroutine()
   {
        _canCollide = false;
        yield return new WaitForSeconds(0.5f);
        _canCollide = true;
   }
   
   private void OnCollisionEnter2D(Collision2D col)
   {
        Debug.Log(message:"Player Collision");
        if (!col.gameObject.CompareTag("Player")) return;
        if(!IsOwner) return;
        if(!_canCollide) return;
        StartCoroutine(routine:CollisionCheckCoroutine());
        if (col.gameObject.TryGetComponent(out PlayerLength playerLength))
        {
            var player1 = new PlayerData()
            {
                Id = OwnerClientId,
                Length = _playerLength.length.Value
            };
            var player2 = new PlayerData()
            {
                Id = playerLength.OwnerClientId,
                Length = playerLength.length.Value
            };
            DetermineCollisionWinnerServerRpc(player1, player2);
        }
        else if(col.gameObject.TryGetComponent(out Tail tail))
        {
            Debug.Log(message:"Tail Collision");
            WinInformationServerRpc(winner:tail.networkedOwner.GetComponent<PlayerController>().OwnerClientId, 
            loser:OwnerClientId);
        }

   }
    struct PlayerData : INetworkSerializable
    {
        public ulong Id;
        public ushort Length;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:
    IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref Length);
    }
    }
}


