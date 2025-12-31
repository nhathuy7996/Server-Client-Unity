using System;
using System.Collections;
using System.Collections.Generic;
using HuynnLib;
using Networking;
using UnityEngine;
using SimpleJSON;

public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] PlayerNetworkSync localPlayerPrefab;
    [SerializeField] NetworkPlayer remotePlayerPrefab;

    [SerializeField] PlayerNetworkSync localPlayer;
    [SerializeField] List<NetworkPlayer> remotePlayers = new List<NetworkPlayer>();
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
        NetworkingPeer.Instant.ListenEvent("server:playerJoined", OnPlayerJoined);
        NetworkingPeer.Instant.ListenEvent("server:newPlayerJoined", OnNewPlayerJoined);
        NetworkingPeer.Instant.ListenEvent("state_update", OnStateUpdateReceived);
    }

    private void OnNewPlayerJoined(string obj)
    {
        Debug.Log("New player joined: " + obj);
        var data = JSON.Parse(obj);
        int playerID = data["id"].AsInt;
        var pos = data["position"];
        float x = pos["x"].AsFloat;
        float y = pos["y"].AsFloat;
        float z = pos["z"].AsFloat;

        UnityMainThread.wkr.AddJob(() =>
        {
            Vector3 position = new Vector3(x, y, z);
            NetworkPlayer remotePlayer = Instantiate(remotePlayerPrefab, position, Quaternion.identity);
            PlayerData playerData = remotePlayer.GetComponent<PlayerData>();
            if (playerData != null)
            {
                playerData.ID = playerID;
            }
            remotePlayers.Add(remotePlayer);
        });
    }

    private void OnPlayerJoined(string obj)
    {
        Debug.Log("Player joined: " + obj);
        var data = JSON.Parse(obj);
        int playerID = data["id"].AsInt;
        var pos = data["position"];
        float x = pos["x"].AsFloat;
        float y = pos["y"].AsFloat;
        float z = pos["z"].AsFloat;

        UnityMainThread.wkr.AddJob(() =>
        {
            Vector3 position = new Vector3(x, y, z);
            this.localPlayer = Instantiate(localPlayerPrefab, position, Quaternion.identity);
            this.localPlayer.PlayerData.ID = playerID;
        });
    }

    void OnStateUpdateReceived(string data)
    {
        Debug.Log("State update received: " + data);
        var array = JSON.Parse(data).AsArray;
        UnityMainThread.wkr.AddJob(() =>
        {
            foreach (JSONNode item in array)
            {
                int id = item["id"].AsInt;
                var pos = item["dirtyState"]["position"];
                if (pos == null) continue;
                if (id == localPlayer.PlayerData.ID)
                {
                    continue; // Skip local player
                }
                float x = pos["x"].AsFloat;
                float y = pos["y"].AsFloat;
                float z = pos["z"].AsFloat;
                Vector3 position = new Vector3(x, y, z);
                NetworkPlayer player = remotePlayers.Find(p => p.playerData.ID == id);
                if (player != null)
                {
                    player.transform.position = position;
                }
                else
                {
                    NetworkPlayer remotePlayer = Instantiate(remotePlayerPrefab, position, Quaternion.identity);
                    PlayerData playerData = remotePlayer.GetComponent<PlayerData>();
                    if (playerData != null)
                    {
                        playerData.ID = id;
                    }
                    remotePlayers.Add(remotePlayer);
                }
            }
        });
    }

    public void SendPlayerVelocity(Vector3 velocity)
    {

        string jsonData = JsonUtility.ToJson(velocity);
        NetworkingPeer.Instant.EmmitEvent("player_velocity", jsonData);
    }
}
