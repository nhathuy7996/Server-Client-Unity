using System.Collections;
using System.Collections.Generic;
using Networking;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField]
    AnimController animController;
    [SerializeField]
    AnimationClip idleClip;
    // Start is called before the first frame update
    void Start()
    {
        NetworkingPeer.Instant.ConnectToServer(OnSocketConnected);
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
           Debug.Log("Game player updates event received with data: " + data);
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
