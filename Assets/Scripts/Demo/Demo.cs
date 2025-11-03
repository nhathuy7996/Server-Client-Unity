using UnityEngine;
using Networking.SocketIo;
using Networking;

public class Demo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkingPeer.Instant.ConnectToServer();
        NetworkingPeer.Instant.ListenEvent("connect", (data) =>
        {
            Debug.Log("Connected to server Unity side ");
        });

        var request = new RequestBase("api/public/getData");
        request.SendAsync((req, isSuccess) =>
        {
            if (isSuccess)
            {
                Debug.Log("Request succeeded: ");
            }
            else
            {
                Debug.LogError("Request failed: ");
            }
        });
    }


}
