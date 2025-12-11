using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using HuynnLib;

namespace Networking
{
    /// <summary>
    /// Manages network connections, player spawning, and synchronization with server
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        [Header("Player Prefabs")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject remotePlayerPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private float spawnRadius = 5f;

        [Header("Network Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool isInGame = false;

        private GameObject localPlayerObject;
        private PlayerNetworkSync localPlayerNetworkSync; // For PlayerMove-based setup
        private Dictionary<int, NetworkPlayer> remotePlayers = new Dictionary<int, NetworkPlayer>();
        private int localPlayerId = -1;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
        }

        /// <summary>
        /// Setup all network event listeners
        /// </summary>
        private void SetupNetworkListeners()
        {
            NetworkingPeer peer = NetworkingPeer.Instant;
            if (peer == null)
            {
                Debug.LogError("[NetworkManager] NetworkingPeer not found!");
                return;
            }

            // Listen for connection
            peer.ListenEvent("connect", OnConnected);

            // Listen for player joined (initial state)
            peer.ListenEvent("server:playerJoined", OnPlayerJoined);

            // Listen for new player spawned
            peer.ListenEvent("server:playerSpawned", OnPlayerSpawned);

            // Listen for player left
            peer.ListenEvent("server:playerLeft", OnPlayerLeft);

            // Listen for players update (continuous sync)
            peer.ListenEvent("server:playersUpdate", OnPlayersUpdate);

            // Listen for disconnect
            peer.ListenEvent("disconnect", OnDisconnected);
        }

        /// <summary>
        /// Connect to server and join the normal map game
        /// </summary>
        public void ConnectAndJoinGame()
        {
            NetworkingPeer peer = NetworkingPeer.Instant;
            if (peer == null)
            {
                Debug.LogError("[NetworkManager] NetworkingPeer not found!");
                return;
            }

            Debug.Log("[NetworkManager] Connecting to server...");
            peer.ConnectToServer(() =>
            {
                Debug.Log("[NetworkManager] Connected! Joining game...");
                this.isConnected = true;
                SetupNetworkListeners();
                JoinGame();
            });
        }

        /// <summary>
        /// Join the normal map game
        /// </summary>
        private void JoinGame()
        {
            NetworkingPeer peer = NetworkingPeer.Instant;
            if (peer == null) return;

            JSONObject gameData = new JSONObject();
            gameData.Add("gameType", "normal");
            gameData.Add("timestamp", DateTime.UtcNow.ToString());

            peer.EmmitEvent("startGame", gameData.ToString());
            Debug.Log("[NetworkManager] Sent join game request");
        }

        /// <summary>
        /// Called when connected to server
        /// </summary>
        private void OnConnected(string data)
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                Debug.Log("[NetworkManager] Connected to server: " + data);
                isConnected = true;
            });
        }

        /// <summary>
        /// Called when disconnected from server
        /// </summary>
        private void OnDisconnected(string reason)
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                Debug.Log("[NetworkManager] Disconnected from server: " + reason);
                isConnected = false;
                isInGame = false;

                // Cleanup all players
                CleanupAllPlayers();
            });
        }

        /// <summary>
        /// Called when player successfully joined game (receives initial state)
        /// </summary>
        private void OnPlayerJoined(string data)
        {
            Debug.Log("[NetworkManager] Player joined data received: " + data);
            UnityMainThread.wkr.AddJob(() =>
            {
                try
                {
                    JSONNode json = JSON.Parse(data);
                    localPlayerId = json["playerId"].AsInt;

                    // Get spawn position from server
                    Vector3 spawnPos = Vector3.zero;
                    if (json["position"] != null)
                    {
                        spawnPos = new Vector3(
                            json["position"]["x"].AsFloat,
                            json["position"]["y"].AsFloat,
                            json["position"]["z"].AsFloat
                        );
                    }

                    Debug.Log($"[NetworkManager] Player joined! My ID: {localPlayerId}, Spawn at: {spawnPos}");

                    // Parse and generate map from mapData
                    if (json["mapData"] != null)
                    {
                        Debug.Log("[NetworkManager] Generating map from server data...");
                        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
                        if (mapGenerator != null)
                        {
                            mapGenerator.GenerateMapFromJSON(json["mapData"].ToString());
                        }
                        else
                        {
                            Debug.LogWarning("[NetworkManager] MapGenerator not found in scene!");
                        }
                    }

                    // Spawn local player at server-provided position
                    SpawnLocalPlayer(spawnPos);

                    // Spawn existing players
                    JSONArray players = json["players"].AsArray;
                    if (players != null)
                    {
                        foreach (JSONNode playerData in players)
                        {
                            int id = playerData["id"].AsInt;
                            Vector3 position = new Vector3(
                                playerData["position"]["x"].AsFloat,
                                playerData["position"]["y"].AsFloat,
                                playerData["position"]["z"].AsFloat
                            );
                            Vector3 velocity = new Vector3(
                                playerData["velocity"]["x"].AsFloat,
                                playerData["velocity"]["y"].AsFloat,
                                playerData["velocity"]["z"].AsFloat
                            );
                            float health = playerData["health"].AsFloat;

                            SpawnRemotePlayer(id, position, velocity, health);
                        }
                    }

                    isInGame = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] Error parsing playerJoined data: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Called when a new player spawns
        /// </summary>
        private void OnPlayerSpawned(string data)
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                try
                {
                    JSONNode json = JSON.Parse(data);
                    int id = json["id"].AsInt;

                    // Don't spawn ourselves
                    if (id == localPlayerId) return;

                    Vector3 position = new Vector3(
                        json["position"]["x"].AsFloat,
                        json["position"]["y"].AsFloat,
                        json["position"]["z"].AsFloat
                    );
                    Vector3 velocity = new Vector3(
                        json["velocity"]["x"].AsFloat,
                        json["velocity"]["y"].AsFloat,
                        json["velocity"]["z"].AsFloat
                    );
                    float health = json["health"].AsFloat;

                    SpawnRemotePlayer(id, position, velocity, health);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] Error parsing playerSpawned data: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Called when a player leaves
        /// </summary>
        private void OnPlayerLeft(string data)
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                try
                {
                    JSONNode json = JSON.Parse(data);
                    int id = json["id"].AsInt;

                    Debug.Log($"[NetworkManager] Player {id} left");
                    DespawnRemotePlayer(id);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] Error parsing playerLeft data: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Called when receiving players update from server
        /// </summary>
        private void OnPlayersUpdate(string data)
        {
            Debug.Log(data);
            UnityMainThread.wkr.AddJob(() =>
            {
                try
                {
                    JSONNode json = JSON.Parse(data);
                    JSONArray players = json["players"].AsArray;
                    long timestamp = json["timestamp"].AsLong;

                    if (players == null)
                    {
                        Debug.LogWarning("[NetworkManager] OnPlayersUpdate: players array is null");
                        return;
                    }

                    int updatedCount = 0;

                    foreach (JSONNode playerData in players)
                    {
                        int id = playerData["id"].AsInt;
                        long sequenceNumber = playerData["sequenceNumber"].AsLong;

                        // UPDATE LOCAL PLAYER position từ server
                        if (id == localPlayerId)
                        {
                            if (localPlayerNetworkSync != null && playerData["position"] != null)
                            {
                                Vector3 serverPosition = new Vector3(
                                    playerData["position"]["x"].AsFloat,
                                    playerData["position"]["y"].AsFloat,
                                    playerData["position"]["z"].AsFloat
                                );
                                localPlayerNetworkSync.UpdateServerPosition(serverPosition, sequenceNumber);
                            }
                            continue;
                        }

                        // Check if remote player exists
                        if (!remotePlayers.ContainsKey(id))
                        {
                            Debug.LogWarning($"[NetworkManager] Player {id} not found in remotePlayers");
                            continue;
                        }

                        NetworkPlayer remotePlayer = remotePlayers[id];

                        // Dynamically update only fields that are present (dirty tracking)
                        UpdatePlayerFields(remotePlayer, playerData, sequenceNumber, timestamp);

                        updatedCount++;
                    }

                    if (updatedCount > 0)
                    {
                        // Debug.Log($"[NetworkManager] Updated {updatedCount} remote players");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] Error parsing playersUpdate data: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Update player fields dynamically based on what's present in JSON
        /// </summary>
        private void UpdatePlayerFields(NetworkPlayer player, JSONNode data, long sequenceNumber = 0, long timestamp = 0)
        {
            // Position
            if (data["position"] != null)
            {
                Vector3 position = new Vector3(
                    data["position"]["x"].AsFloat,
                    data["position"]["y"].AsFloat,
                    data["position"]["z"].AsFloat
                );
                player.UpdatePosition(position, sequenceNumber);
            }

            // Velocity
            if (data["velocity"] != null)
            {
                Vector3 velocity = new Vector3(
                    data["velocity"]["x"].AsFloat,
                    data["velocity"]["y"].AsFloat,
                    data["velocity"]["z"].AsFloat
                );
                player.UpdateVelocity(velocity);
            }

            // Health
            if (data["health"] != null)
            {
                player.UpdateHealth(data["health"].AsFloat);
            }

            // Speed (if exists)
            if (data["speed"] != null)
            {
                player.UpdateSpeed(data["speed"].AsFloat);
            }

            // Easy to add more fields here as needed
            // Example: 
            // if (data["mana"] != null) player.UpdateMana(data["mana"].AsFloat);
            // if (data["stamina"] != null) player.UpdateStamina(data["stamina"].AsFloat);
        }

        /// <summary>
        /// Spawn local player at specified position
        /// </summary>
        private void SpawnLocalPlayer(Vector3 spawnPos)
        {
            if (localPlayerObject != null)
            {
                Debug.LogWarning("[NetworkManager] Local player already exists!");
                return;
            }

            localPlayerObject = Instantiate(localPlayerPrefab, spawnPos, Quaternion.identity);

            // Try to get PlayerNetworkSync first (for PlayerMove-based setup)
            localPlayerNetworkSync = localPlayerObject.GetComponent<PlayerNetworkSync>();
            if (localPlayerNetworkSync != null)
            {
                localPlayerNetworkSync.InitializeAsLocalPlayer(localPlayerId);
                Debug.Log($"[NetworkManager] Local player (PlayerMove) spawned at {spawnPos}");
                return;
            }

            Debug.LogError("[NetworkManager] Local player prefab missing PlayerNetworkSync or PlayerController!");
        }

        /// <summary>
        /// Spawn remote player
        /// </summary>
        private void SpawnRemotePlayer(int id, Vector3 position, Vector3 velocity, float health)
        {
            if (remotePlayers.ContainsKey(id))
            {
                Debug.LogWarning($"[NetworkManager] Remote player {id} already exists!");
                return;
            }

            GameObject playerObj = Instantiate(remotePlayerPrefab, position, Quaternion.identity);
            NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();

            if (networkPlayer != null)
            {
                networkPlayer.playerId = id;
                networkPlayer.UpdateState(position, velocity, health);
                remotePlayers.Add(id, networkPlayer);

                Debug.Log($"[NetworkManager] Remote player {id} spawned at {position}");
            }
            else
            {
                Debug.LogError("[NetworkManager] Remote player prefab missing NetworkPlayer!");
                Destroy(playerObj);
            }
        }

        /// <summary>
        /// Despawn remote player
        /// </summary>
        private void DespawnRemotePlayer(int id)
        {
            if (remotePlayers.ContainsKey(id))
            {
                NetworkPlayer player = remotePlayers[id];
                remotePlayers.Remove(id);
                Destroy(player.gameObject);
                Debug.Log($"[NetworkManager] Remote player {id} despawned");
            }
        }

        /// <summary>
        /// Cleanup all players
        /// </summary>
        private void CleanupAllPlayers()
        {
            if (localPlayerObject != null)
            {
                Destroy(localPlayerObject);
                localPlayerObject = null;
                localPlayerNetworkSync = null;
            }

            foreach (var player in remotePlayers.Values)
            {
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
            }
            remotePlayers.Clear();
        }

        /// <summary>
        /// Send player velocity update to server
        /// Server will calculate position based on velocity
        /// </summary>
        public void SendPlayerVelocity(Vector3 velocity)
        {
            if (!isConnected || !isInGame) return;

            NetworkingPeer peer = NetworkingPeer.Instant;
            if (peer == null) return;

            var data = new
            {
                velocity = velocity
            };

            peer.EmmitEvent("client:updateVelocity", JSON.ToJSON(data));
        }

        /// <summary>
        /// [DEPRECATED] For PlayerController compatibility (not used in PlayerMove setup)
        /// </summary>
        public void SendPlayerPosition(Vector3 position, Vector3 velocity)
        {
            SendPlayerVelocity(velocity);
        }

        private void OnDestroy()
        {
            CleanupAllPlayers();
        }

        // Public getters
        public bool IsConnected => isConnected;
        public bool IsInGame => isInGame;
        public int LocalPlayerId => localPlayerId;
        public GameObject LocalPlayerObject => localPlayerObject;
        public PlayerNetworkSync LocalPlayerNetworkSync => localPlayerNetworkSync;
    }
}
