using Unity.Netcode;
using UnityEngine;

class ConnectionApprovalHandler : MonoBehaviour //SCRIPT para limitar cantidad de jugadores.
{
    [SerializeField] private int maxPlayers;

    int playersInGame = 0;
    NetworkManager m_NetworkManager;


    void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        if (m_NetworkManager != null)
        {
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_NetworkManager.ConnectionApprovalCallback = ApprovalCheck;
        }
    }

    void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if ((m_NetworkManager.ConnectedClients.Count + playersInGame) > maxPlayers)
        {
            Debug.Log("Rejecting player since server is full");
            response.Approved = false;
            response.Reason = "Server is full";
            return;
        }

        response.Approved = true;
        playersInGame++;
    }

    void OnClientDisconnectCallback(ulong obj)
    {
        if (!m_NetworkManager.IsServer && m_NetworkManager.DisconnectReason != string.Empty)
        {
            Debug.Log($"Server declined connection because: {m_NetworkManager.DisconnectReason}");
        }
    }
}