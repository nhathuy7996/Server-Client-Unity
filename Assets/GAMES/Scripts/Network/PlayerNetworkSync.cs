using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviour
{

    PlayerData _playerData;
    public PlayerData PlayerData => _playerData ??= GetComponent<PlayerData>();
    PlayerMove _playerMove;
    Vector3 _lastSentVelocity;
    float _lastUpdateTime;

    [SerializeField] private float _positionUpdateRate = 0.05f; // Send every 50ms
    [SerializeField] private float _maxPositionError = 2f; // Max error before snap
    [SerializeField] private float _reconciliationSpeed = 5f; // Lerp speed for smooth correction

    Vector3 _serverPosition;
    int _lastReceivedSequenceNumber = -1;


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
        CorrectPosition();
    }

    void SendPositionUpdate()
    {
        // Send _playerData.Velocity to server
        if (Time.time - _lastUpdateTime < _positionUpdateRate)
            return;
        _lastUpdateTime = Time.time;

        Vector3 currentVelocity = _playerData.Velocity;

        float velocityDelta = Vector3.Distance(currentVelocity, _lastSentVelocity);

        if (velocityDelta > 0.01f) // Lower threshold for better responsiveness
        {
            _lastSentVelocity = currentVelocity;
            NetworkManager.Instant.SendPlayerVelocity(currentVelocity);
        }
    }

    public void ApplyServerPosition(Vector3 serverPosition, int sequenceNumber = 0)
    {
        if (sequenceNumber <= _lastReceivedSequenceNumber)
        {
            return; // Ignore out-of-order update
        }
        _lastReceivedSequenceNumber = sequenceNumber;
        _serverPosition = serverPosition;
    }

    void CorrectPosition()
    {
        if (_serverPosition == Vector3.zero) return; // Wait for first server update

        float positionError = Vector3.Distance(transform.position, _serverPosition);
        Debug.LogError("Position error: " + positionError);

        if (positionError > _maxPositionError)
        {
            // Snap for large errors (e.g., teleport or major correction)
            transform.position = _serverPosition;
        }
        else if (positionError > 0.01f)
        {
            // Lerp for small errors (smooth reconciliation)
            transform.position = Vector3.Lerp(transform.position, _serverPosition, Time.fixedDeltaTime * _reconciliationSpeed);
        }
    }

}
