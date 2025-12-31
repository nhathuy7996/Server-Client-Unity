using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviour
{

    PlayerData _playerData;
    public PlayerData PlayerData => _playerData;
    PlayerMove _playerMove;
    Vector3 _lastSentVelocity;
    float _lastUpdateTime;

    float _positionUpdateRate;


    // Start is called before the first frame update
    void Awake()
    {
        _playerData = GetComponent<PlayerData>();
        _playerMove = GetComponent<PlayerMove>();
    }

    void FixedUpdate()
    {
        //if (!isLocalPlayer) return;

        SendPositionUpdate();
    }

    void SendPositionUpdate()
    {
        // Send _playerData.Velocity to server
        if (Time.time - _lastUpdateTime < _positionUpdateRate)
            return;
        _lastUpdateTime = Time.time;

        Vector3 currentVelocity = _playerData.Velocity;

        float velocityDelta = Vector3.Distance(currentVelocity, _lastSentVelocity);

        if (velocityDelta > 0.1f)
        {
            _lastSentVelocity = currentVelocity;
            NetworkManager.Instant.SendPlayerVelocity(currentVelocity);

            _lastSentVelocity = currentVelocity;
            _lastUpdateTime = Time.time;
        }
    }

}
