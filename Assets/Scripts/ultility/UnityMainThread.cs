using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HuynnLib
{
    internal class UnityMainThread : MonoBehaviour
    {
        private static UnityMainThread _instance;
        internal static UnityMainThread wkr
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("UnityMainThread");
                    _instance = obj.AddComponent<UnityMainThread>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
        Queue<Action> jobs = new Queue<Action>();

        void Update()
        {
            while (jobs.Count > 0)
            {
                try
                {
                    jobs.Dequeue().Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error executing job in main thread: " + ex.Message);
                }

            }
        }

        internal void AddJob(Action newJob)
        {
            jobs.Enqueue(newJob);
        }

    }
}