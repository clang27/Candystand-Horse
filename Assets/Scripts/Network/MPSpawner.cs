using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public struct NetworkInputData : INetworkInput {
    public const byte TRICK1 = 0b000000001;
    public const byte TRICK2 = 0b000000010;
    public const byte TRICK3 = 0b000000100;
    public const byte TRICK4 = 0b000001000;
    public const byte TRICK5 = 0b000010000;
    public const byte TRICK6 = 0b000100000;
    public const byte TRICK7 = 0b001000000;
    public const byte TRICK8 = 0b010000000;
    public const ushort TRICK9 = 0b100000000;
    
    public bool BallMoving, BallShooting;
    public Vector3 BallAimPoint;
    
    public bool BoomboxMoving;
    public Vector3 BoomboxAimPoint;

    public ushort Tricks; // i.e. 0b000001000
    public override string ToString() {
        return BallMoving + " " + BallShooting + " " + BoomboxMoving + "\n" + Tricks;
    }
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
    public static MPTricks Tricks { get; set; }
    
    public static readonly bool[] TricksClicked = new bool[9];
    private readonly bool[] _tricksClicked = new bool[9];

    public List<MPBasketball> Balls => _spawnedPlayers
        .Select(sp => sp.Value.GetComponent<MPBasketball>()).ToList();

    private void Awake() {
        _localBasketball = FindObjectOfType<BasketballFlick>();
        _localBoombox = FindObjectOfType<Boombox>();
        _localTimer = FindObjectOfType<TimerUi>();
    }
    
    private void Update() {
        for (var i = 0; i < _tricksClicked.Length; i++) {
            _tricksClicked[i] = _tricksClicked[i] || TricksClicked[i];
            TricksClicked[i] = false;
        }
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
        
        TrickShotsSelector.Instance.ActivateButton(false);
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
        
        TrickShotsSelector.Instance.ActivateButton(false);
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
       
        var data = new NetworkInputData() {
            BallMoving = Ball.Moving,
            BallShooting = Ball.Shooting,
            BallAimPoint = Ball.AimPoint,
            BoomboxMoving = Boombox.Moving,
            BoomboxAimPoint = Boombox.AimPoint
        };

        if (_tricksClicked[0])
            data.Tricks |= NetworkInputData.TRICK1;
        if (_tricksClicked[1])
            data.Tricks |= NetworkInputData.TRICK2;
        if (_tricksClicked[2])
            data.Tricks |= NetworkInputData.TRICK3;
        if (_tricksClicked[3])
            data.Tricks |= NetworkInputData.TRICK4;
        if (_tricksClicked[4])
            data.Tricks |= NetworkInputData.TRICK5;
        if (_tricksClicked[5])
            data.Tricks |= NetworkInputData.TRICK6;
        if (_tricksClicked[6])
            data.Tricks |= NetworkInputData.TRICK7;
        if (_tricksClicked[7])
            data.Tricks |= NetworkInputData.TRICK8;
        if (_tricksClicked[8])
            data.Tricks |= NetworkInputData.TRICK9;

        for (var i = 0; i < _tricksClicked.Length; i++)
            _tricksClicked[i] = false;

        input.Set(data);
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
        TrickShotsSelector.Instance.ActivateButton(false);
        GameUiManager.Instance.ShowLoading(false);
    }

    public void OnSceneLoadStart(NetworkRunner runner) {
        TrickShotsSelector.Instance.ActivateButton(false);
        _localBasketball.ResetPosition(Vector3.up * 1000f);
        _localBasketball.gameObject.SetActive(false);
        _localBoombox.gameObject.SetActive(false);
        _localTimer.gameObject.SetActive(false);
    }
}
