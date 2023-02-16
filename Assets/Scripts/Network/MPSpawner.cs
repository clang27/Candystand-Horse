using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public struct NetworkInputData : INetworkInput {
    public bool Moving, Shooting;
    public Vector3 AimPoint;
}

public class MPSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    [SerializeField] private NetworkPrefabRef _ballPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedBalls = new();
    private MPBasketball _myBall;
    private NetworkRunner _runner;
    public async void StartGame(bool host) {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        GameUiManager.Instance.ShowLoading(true);
        
        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs() {
            Address = NetAddress.Any(),
            GameMode = (host) ? GameMode.Host : GameMode.Client,
            SessionName = "TestRoom",
            Scene = 0,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public async void EndGame() {
        if (!_runner) return;
        
        _spawnedBalls.Clear();
        await _runner.Shutdown();
        Destroy(_runner);
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (runner.IsServer) {
            GameUiManager.Instance.ShowLoading(false);
            var spawnPosition = MenuManager.Instance.CurrentLevel.ballRespawnPoint;
            var networkPlayerObject = runner.Spawn(_ballPrefab, spawnPosition, Quaternion.identity, player);
            if (_spawnedBalls.Count == 0) {
                _myBall = networkPlayerObject.GetComponent<MPBasketball>();
            }
            _spawnedBalls.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (_spawnedBalls.TryGetValue(player, out var networkObject)) {
            runner.Despawn(networkObject);
            _spawnedBalls.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        if (!_myBall) return;
        
        var data = new NetworkInputData {
            Moving = _myBall.Moving,
            Shooting = _myBall.Shooting,
            AimPoint = _myBall.AimPoint
        };

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
        
    }

    public void OnConnectedToServer(NetworkRunner runner) {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner) {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
        
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner) {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner) {
        
    }
}
