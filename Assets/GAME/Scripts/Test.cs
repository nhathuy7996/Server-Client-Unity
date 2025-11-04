using System.Collections;
using System.Collections.Generic;
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
