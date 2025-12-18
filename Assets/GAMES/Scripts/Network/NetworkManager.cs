using System;
using System.Collections;
using System.Collections.Generic;
using HuynnLib;
using Networking;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkingPeer peer = NetworkingPeer.Instant;
        peer.ConnectToServer(() =>
        {
            Debug.Log("Connected to server!");
            SetupNetworkListener();
            JoinMap();

        });
    }

    void JoinMap()
    {
        NetworkingPeer.Instant.EmmitEvent("startGame", "{}");
    }

    void SetupNetworkListener()
    {
        NetworkingPeer.Instant.ListenEvent("state_update", OnStateUpdateReceived);
    }

    void OnStateUpdateReceived(string data)
    {
        Debug.Log("State update received: " + data);
        // Parse and apply position update
    }

    public void SendPlayerVelocity(Vector3 velocity)
    {

        string jsonData = JsonUtility.ToJson(velocity);
        NetworkingPeer.Instant.EmmitEvent("player_velocity", jsonData);
    }
}
