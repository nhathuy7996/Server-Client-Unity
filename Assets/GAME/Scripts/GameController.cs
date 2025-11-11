using System;
using System.Collections;
using System.Collections.Generic;
using HuynnLib;
using Networking;
using SimpleJSON;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] CamControl camControl;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] PlayerData otherPlayerPrefab;
    [SerializeField] Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    [SerializeField]
    int playerID = 0;
    // Start is called before the first frame update
    void Start()
    {
        var reqLogin = new Networking.RequestBase("api/auth/", $"{{ \"userId\": {DateTime.Now.Millisecond} }}");
        reqLogin.SendAsync((req, isSuccess) =>
        {
            if (isSuccess)
            {
                Debug.Log("Login successful: " + req.response);
                var json = SimpleJSON.JSON.Parse(req.response);
                Module.jwt = json["data"]["jwt"].Value;

                Debug.Log("JWT Token: " + Module.jwt);
                NetworkingPeer.Instant.ConnectToServer(OnSocketConnected);
            }

        });

    }

    void OnSocketConnected()
    {
        Debug.Log("Connected to server at " + Module.baseUrl);
        NetworkingPeer.Instant.EmmitEvent("startGame", "{}");
        NetworkingPeer.Instant.ListenEvent("game:joined", OnPlayerJoined);

        NetworkingPeer.Instant.ListenEvent("game:playerJoined", OnOtherPlayerJoined);

        NetworkingPeer.Instant.ListenEvent("game:playerLeft", OnPlayerLeft);


        NetworkingPeer.Instant.ListenEvent("game:allPlayersState", OnGetAllPlayersState);

        NetworkingPeer.Instant.ListenEvent("game:playerUpdates", OnPlayerUpdate);


    }


    void OnPlayerJoined(string data)
    {
        var dataJson = SimpleJSON.JSON.Parse(data);
        playerID = dataJson["playerId"].AsInt;
        UnityMainThread.wkr.AddJob(() =>
        {
            var player = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            camControl.target = player.transform;
        });
    }

    void OnOtherPlayerJoined(string data)
    {
        var dataJson = SimpleJSON.JSON.Parse(data);
        var playerID = dataJson["playerId"].AsInt;
        if (this.playerID == playerID)
        {
            return;
        }
        UnityMainThread.wkr.AddJob(() => AutoFillPlayerNotExist(otherPlayerPrefab, playerID));
    }

    void OnPlayerLeft(string data)
    {
        var dataJson = SimpleJSON.JSON.Parse(data);
        var playerID = dataJson["playerId"].AsInt;
        if (!this.players.ContainsKey(playerID))
        {
            return;
        }
        UnityMainThread.wkr.AddJob(() =>
        {
            Destroy(this.players[playerID].gameObject);
            this.players.Remove(playerID);
        });
    }

    void OnGetAllPlayersState(string data)
    {
        var dataJson = SimpleJSON.JSON.Parse(data);
        var playersArray = dataJson["players"].AsArray;
        UnityMainThread.wkr.AddJob(() =>
        {
            for (int i = 0; i < playersArray.Count; i++)
            {
                var playerData = playersArray[i];
                var playerID = playerData["id"].AsInt;
                if (this.playerID == playerID)
                {
                    continue;
                }

                AutoFillPlayerNotExist(otherPlayerPrefab, playerID);


                var position = new Vector3(playerData["position"]["x"].AsFloat, playerData["position"]["y"].AsFloat, playerData["position"]["z"].AsFloat);
                this.players[playerID].transform.position = position;

            }

        });
    }

    void OnPlayerUpdate(string data)
    {
        var dataJson = SimpleJSON.JSON.Parse(data);
        UnityMainThread.wkr.AddJob(() =>
        {
            for (int i = 0; i < dataJson.AsArray.Count; i++)
            {
                var playerData = dataJson.AsArray[i];
                var playerID = playerData["id"].AsInt;
                if (this.playerID == playerID)
                {
                    continue;
                }

                AutoFillPlayerNotExist(otherPlayerPrefab, playerID);

                if (playerData["position"] != null)
                {
                    var position = new Vector3(playerData["position"]["x"].AsFloat, playerData["position"]["y"].AsFloat, playerData["position"]["z"].AsFloat);
                    this.players[playerID].transform.position = position;
                }

                if (playerData["velocity"] != null)
                {

                    var velocity = new Vector3(playerData["velocity"]["x"].AsFloat, playerData["velocity"]["y"].AsFloat, playerData["velocity"]["z"].AsFloat);
                    this.players[playerID].velocity = velocity;

                    // Rotate player to face velocity direction
                    if (velocity.magnitude > 0.1f)
                    {
                        this.players[playerID].transform.rotation = Quaternion.LookRotation(velocity.normalized);
                    }
                }

            }
        });

    }

    void AutoFillPlayerNotExist(PlayerData playerPrefab, int playerID)
    {
        if (!this.players.ContainsKey(playerID))
        {
            var player = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            player.name = "Player_" + playerID;
            player.playerID = playerID;
            this.players.Add(playerID, player);
        }
    }
}
