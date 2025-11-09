using System;
using System.Collections;
using System.Collections.Generic;
using HuynnLib;
using Networking;
using SimpleJSON;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField]
    AnimController animController;
    [SerializeField]
    AnimationClip idleClip;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] List<Transform> players;
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
            else
            {

            }
        });

    }

    void OnSocketConnected()
    {
        Debug.Log("Connected to server at " + Module.baseUrl);
        NetworkingPeer.Instant.EmmitEvent("startGame", "{}");
        NetworkingPeer.Instant.ListenEvent("game:allPlayersState", (data) =>
        {
            Debug.Log("Game started event received with data: " + data);
        });

        NetworkingPeer.Instant.ListenEvent("game:playerUpdates", (data) =>
       {

           var dataJson = SimpleJSON.JSON.Parse(data);
           UnityMainThread.wkr.AddJob(() =>
           {
               for (int i = 0; i < dataJson.AsArray.Count; i++)
               {
                   var playerData = dataJson.AsArray[i];
                   var playerID = playerData["id"].AsInt;
                   if (this.players[playerID] == null)
                   {
                       this.players[playerID] = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity).transform;
                   }
                   this.players[playerID].position = new Vector3(playerData["position"]["x"].AsFloat, playerData["position"]["y"].AsFloat, playerData["position"]["z"].AsFloat);
                   Debug.Log($"Player Update - ID: {playerData["id"].AsInt}, Position: ({playerData["position"]["x"].AsFloat}, {playerData["position"]["y"].AsFloat}, {playerData["position"]["z"].AsFloat})");
               }
           });

       });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key was pressed.");
            animController.OverrideSpecificClip(AnimController.AnimState.Idle, idleClip);
        }
    }
}
