using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public struct NetworkInputData : INetworkInput {
    public bool BallMoving, BallShooting;
    public Vector3 BallAimPoint;
    
    public bool BoomboxMoving;
    public Vector3 BoomboxAimPoint;

    // TODO: Send flags for tricks clicked and have client register them
    public MPTrickShot TrickOne, TrickTwo, TrickThree, TrickFour, TrickFive, TrickSix;
}

public class MPSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    [SerializeField] private NetworkPrefabRef _ballPrefab, _boomboxPrefab, _timerPrefab;
    public bool IsServer => _runner && _runner.IsServer;
    public bool IsShuttingDown => _runner && _runner.IsShutdown;
    public NetworkRunner Runner => _runner;
    
    private static readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private NetworkRunner _runner;

    private string _roomCode;
    private BasketballFlick _localBasketball;
    private Boombox _localBoombox;
    private TimerUi _localTimer;
    public static MPBasketball Ball { get; set; }
    public static MPBoombox Boombox { get; set; }
    public static MPTimer Timer { get; set; }
    
    public List<MPBasketball> Balls => _spawnedPlayers
        .Select(sp => sp.Value.GetComponent<MPBasketball>()).ToList();

    private void Awake() {
        _localBasketball = FindObjectOfType<BasketballFlick>();
        _localBoombox = FindObjectOfType<Boombox>();
        _localTimer = FindObjectOfType<TimerUi>();
    }
    
    public async void StartGame(bool host) {
        GameUiManager.Instance.ShowLoading(true);

        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _roomCode = (host) ? 
            Guid.NewGuid().ToString().ToUpper().Substring(0, 5) : 
            MenuManager.Instance.RoomCode.ToUpper();

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs() {
            Address = NetAddress.Any(),
            GameMode = (host) ? GameMode.Host : GameMode.Client,
            SessionName = _roomCode,
            Scene = 0,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public async void EndGame() {
        if (!_runner) return;
        
        await _runner.Shutdown();
    }

    public void StartMatch() {
        if (!IsServer) return;

        Ball.GameStarted = true;
        Ball.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        Boombox.ResetPosition(MenuManager.Instance.CurrentLevel.boomboxRespawnPoint);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (!IsServer) return;
        
        GameUiManager.Instance.ShowLobbyInfo(_roomCode, _spawnedPlayers.Count > 0);

        var ballSpawn = MenuManager.Instance.CurrentLevel.ballRespawnPoint;
        var networkPlayerObject = runner.Spawn(_ballPrefab, ballSpawn, Quaternion.identity, player);
        _spawnedPlayers.Add(player, networkPlayerObject);

        if (!Boombox) {
            var boomboxSpawn = MenuManager.Instance.CurrentLevel.boomboxRespawnPoint;
            Boombox = runner.Spawn(_boomboxPrefab, boomboxSpawn, Quaternion.identity, player).GetComponent<MPBoombox>();
            Boombox.Active = MenuManager.Instance.BoomboxEnabled;
        }
        
        if (!Timer) {
            var timerSpawn = MenuManager.Instance.CurrentLevel.timerRespawnPoint;
            Timer = runner.Spawn(_timerPrefab, timerSpawn, Quaternion.identity, player).GetComponent<MPTimer>();
            Timer.Seconds = MenuManager.Instance.ShotClock;
        }

        // Hide other basketballs away while waiting on host to start game
        if (_spawnedPlayers.Count > 1) {
            var mb = networkPlayerObject.GetComponent<MPBasketball>();
            mb.ResetPosition(Vector3.up * 1000f);
            mb.ResetRigidbody();
        }
    }
    
    public void OnConnectedToServer(NetworkRunner runner) {
        if (IsServer) return;
        
        GameUiManager.Instance.ShowLobbyInfo(_roomCode, false);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (!_spawnedPlayers.TryGetValue(player, out var networkObject)) return;
        
        runner.Despawn(networkObject);
        _spawnedPlayers.Remove(player);

        if (_spawnedPlayers.Count <= 1) {
            _runner.Shutdown();
        } else {
            GameUiManager.Instance.UpdateMPScore(Balls.Select(b => b.Player).ToList());
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        if (!Ball || !Boombox || !Timer) return;

        // TODO: Send flags for tricks clicked and have client register them
        input.Set(new NetworkInputData() {
            BallMoving = Ball.Moving,
            BallShooting = Ball.Shooting,
            BallAimPoint = Ball.AimPoint,
            BoomboxMoving = Boombox.Moving,
            BoomboxAimPoint = Boombox.AimPoint,
            TrickOne = Ball.ClientTricks[0],
            TrickTwo = Ball.ClientTricks[1],
            TrickThree = Ball.ClientTricks[2],
            TrickFour = Ball.ClientTricks[3],
            TrickFive = Ball.ClientTricks[4],
            TrickSix = Ball.ClientTricks[5]
        });
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
        CleanUp();
        
        GameUiManager.Instance.ShowBanner((shutdownReason == ShutdownReason.ConnectionRefused) ? "Match In Progress" : "Disconnected", 2f);
        GameManager.Instance.GoToPractice();
    }

    private void CleanUp() {
        _spawnedPlayers.Clear();
        
        _localBasketball.gameObject.SetActive(true);
        _localBoombox.gameObject.SetActive(true);
        _localTimer.gameObject.SetActive(true);
        
        GameUiManager.Instance.ShowLoading(false);
        GameUiManager.Instance.HideLobbyInfo();
        GameUiManager.Instance.UpdateMPScore(new List<MPPlayer>());
        
        Destroy(_runner);
        Destroy(GetComponent<NetworkSceneManagerDefault>());
        Destroy(GetComponent<NetworkPhysicsSimulation2D>());
        Destroy(GetComponent<HitboxManager>());

        Ball = null;
        Boombox = null;
        Timer = null;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner) {
        CleanUp();
        GameUiManager.Instance.ShowBanner("Disconnected", 2f);
        GameManager.Instance.GoToPractice();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
        if (runner.IsClient) return;
        
        if (GameManager.Instance.Mode == GameType.OnlineLobby)
            request.Accept();
        else
            request.Refuse();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
        CleanUp();
        GameUiManager.Instance.ShowBanner("Failure", 2f);
        GameManager.Instance.GoToPractice();
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
        GameUiManager.Instance.ShowLoading(false);
        TrickShotsSelector.Instance.ActivateButton(false);
    }

    public void OnSceneLoadStart(NetworkRunner runner) {
        _localBasketball.ResetPosition(Vector3.up * 1000f);
        _localBasketball.gameObject.SetActive(false);
        _localBoombox.gameObject.SetActive(false);
        _localTimer.gameObject.SetActive(false);
    }
}
