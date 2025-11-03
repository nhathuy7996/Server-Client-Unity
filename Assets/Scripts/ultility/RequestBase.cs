using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using HuynnLib;

namespace Networking
{
    public class RequestBase
    {

        protected UnityWebRequest request;

        public RequestBase(string endPoin)
        {
            this.request = UnityWebRequest.Get(Module.baseUrl + endPoin);
        }

        public RequestBase(string endPoin, string postData, RequestType type = RequestType.POST)
        {
            if (!string.IsNullOrEmpty(postData))
            {

                this.request = new UnityWebRequest(Module.baseUrl + endPoin, type.ToString());
                byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            }
            else
                this.request = UnityWebRequest.PostWwwForm(Module.baseUrl + endPoin, "");

        }


        public RequestBase setHeader(string name, string value)
        {
            this.request.SetRequestHeader(name, value);
            return this;
        }


        public async Task<RequestBase> Send(System.Action<RequestBase, bool> onDone = null)
        {

            this.request.SetRequestHeader("Content-Type", "application/json");
            this.request.SetRequestHeader("Accept", "application/json");
            this.request.SetRequestHeader("Authorization", $"Bearer {Module.jwt}");
            this.request.SendWebRequest();

#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log("WebGL detected, using coroutine for request waiting");
                return this;
#endif

            float timer = 0;
            while (!this.request.isDone && timer < 240000)
            {
                await Task.Delay(10);
                timer += 10;
            }

            if (this.request.result == UnityWebRequest.Result.Success)
            {

                if (onDone != null)
                {
                    try
                    {
                        UnityMainThread.wkr.AddJob(() =>
                        {
                            onDone(this, true);
                        });

                    }
                    catch (Exception e)
                    {
                        Debug.LogError("request--> " + this.request.uri + "---- invoke done error: \n " + e);
                    }

                }
            }
            else
            {

                if (onDone != null)
                {
                    try
                    {
                        UnityMainThread.wkr.AddJob(() =>
                        {
                            onDone(this, false);
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("request--> " + this.request.uri + "---- invoke fail error: \n " + e);
                    }

                }
            }

            return this;
        }

        public string getResHeader(string key) => this.request.GetResponseHeader(key);

        public bool isDone => this.request.isDone;

        public float progress => this.request.downloadProgress;

        public string access_token => this.getResHeader("x-access-token");

        public string response => this.request.downloadHandler.text;

        public long responseCode => this.request.responseCode;

        public UnityWebRequest.Result result => this.request.result;

        public enum RequestType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }

}