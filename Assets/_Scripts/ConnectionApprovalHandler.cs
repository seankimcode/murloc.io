using Unity.Netcode;
using UnityEngine;

public class ConnectionApprovalHandler : MonoBehaviour
{
    private const int MaxPlayers = 10;
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    // Update is called once per frame
    void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null;
        if (NetworkManager.Singleton.ConnectedClients.Count >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "Server is full";
        }

        response.Pending = false;
    }
}
