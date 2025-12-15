using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviour
{

    PlayerData _playerData;
    PlayerMove _playerMove;

    Vector3 _lastSeenPosition;
    Vector3 _lastSentVelocity;
    float _lastUpdateTime;

    float _positionUpdateRate;

    [Header("Player Info")]

    public int playerId = -1;
    public bool isLocalPlayer = false;

    // Start is called before the first frame update
    void Start()
    {
        _playerData = GetComponent<PlayerData>();
        _playerMove = GetComponent<PlayerMove>();

        _lastSeenPosition = transform.position;
    }

    void FixedUpdate()
    {
        //if (!isLocalPlayer) return;

        SendPositionUpdate();
    }

    void InitializeAsPlayer(int id, bool isLocal = false)
    {
        isLocalPlayer = isLocal;
        playerId = id;

        if (isLocalPlayer)
        {
            // Enable local player controls
            _playerMove.enabled = true;
        }
        else
        {
            // Disable local player controls for remote players
            _playerMove.enabled = false;
        }

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
            _lastSeenPosition = transform.position;
            _lastUpdateTime = Time.time;
        }
    }

}
