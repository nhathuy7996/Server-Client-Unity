using System;
using System.Collections;
using System.Collections.Generic;
using HuynnLib;
using Networking.SocketIo;
using SimpleJSON;
using UnityEngine;

namespace Networking
{
    public class NetworkingPeer : Singleton<NetworkingPeer>
    {

        Networking.SocketIo.Socket _socket;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        public void ConnectToServer(Action onConnected = null)
        {
            Debug.Log("Connecting to server...");
            string data = "{\"auth\":{\"token\":\"" + Module.jwt + "\"} }";

            _socket = Networking.SocketIo.SocketIo.establishSocketConnection(Module.baseUrl, data);
            _socket.connect();
        }

        void OnDestroy()
        {
            _socket?.disconnect();
        }

        public void EmmitEvent(string name, string data)
        {
            _socket?.emit(name, data);
        }

        public void ListenEvent(string name, Action<string> callback)
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                _socket?.on(name, callback);
            });
        }

        public void RemoveListener(string name, Action<string> callback)
        {
            _socket?.off(name, callback);
        }


    }
}
