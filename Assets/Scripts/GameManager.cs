using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public enum TurnPhase {
    Moving, Responding, Charging, Shooting, Resting, Transitioning, Finished
}

public class GameManager : MonoBehaviour {
    [SerializeField] private AudioClip _applause, _whistle;
    public static GameManager Instance;
    public TurnPhase TurnPhase { get; set; }
    private BasketballFlick _localBasketball;
    private Boombox _localBoombox;
    private Transform _cameraTransform;
    private AudioSource _audioSource;
    private Blindfold _blindfold;
    private AiController _aiController;
    private List<Player> _players = new();
    private bool _shotMade { get; set; }
    private bool _incorrectShot { get; set; }
    public GameType Mode { get; private set; }
    private Coroutine _pauseRoutine;
    private MPSpawner _mpSpawner;
    private bool _transitioning;
    
    private void Awake() {
        Instance = this;

        _audioSource = GetComponent<AudioSource>();
        _localBasketball = FindObjectOfType<BasketballFlick>();
        _aiController = _localBasketball.GetComponent<AiController>();
        _localBoombox = FindObjectOfType<Boombox>();
        _blindfold = FindObjectOfType<Blindfold>();
        _cameraTransform = FindObjectOfType<Camera>().transform;

        _mpSpawner = FindObjectOfType<MPSpawner>();
    }

    private void Start() {
        GoToPractice();
    }
    public void GoToPractice() {
        Mode = GameType.Practice;
        ResetScene();
        GameUiManager.Instance.ShowPracticeInfo(true);
        _localBasketball.ChangeColor(Color.HSVToRGB(0.07f, 1f, 1f));
    }

    public void GoToLocal(int playerCount) {
        Mode = GameType.Local;
        ResetScene();
        
        for (var i = 0; i < playerCount; i++)
            _players.Add(new Player("P" + (i + 1), playerCount));

        _players[0].IsTurn = true;
        _localBasketball.ChangeColor(Color.red);
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }

    public void GoToAi() {
        Mode = GameType.Ai;
        ResetScene();
        
        _players.Add(new Player("P1",  2));
        _players.Add(new Player("AI",  2));
        
        _players[0].IsTurn = true;
        _players[1].IsAi = true;
        _localBasketball.ChangeColor(Color.red);
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }
    public void GoToOnlineLobby(bool host) {
        if (!host && (string.IsNullOrEmpty(MenuManager.Instance.RoomCode) || 
                      MenuManager.Instance.RoomCode.Length < 5 || MenuManager.Instance.RoomCode.Contains("-"))) {
            _audioSource.PlayOneShot(_whistle);
            return;
        }
        
        Mode = GameType.OnlineLobby;
        ResetScene();
        TrickShotsSelector.Instance.ActivateButton(false);
        StartCoroutine(WaitForServerShutdown(host));
    }
    
    private IEnumerator WaitForServerShutdown(bool host) {
        if (_mpSpawner.IsShuttingDown) {
            GameUiManager.Instance.ShowLoading(true);
        }
        while (_mpSpawner.IsShuttingDown) {
            yield return null;
        }
        
        GameUiManager.Instance.ShowLoading(false);
        Mode = GameType.OnlineLobby;
        _mpSpawner.StartGame(host);
    }
    
    public void GoToOnlineMatch() {
        Mode = GameType.OnlineMatch;
        
        GameUiManager.Instance.HideLobbyInfo();
        
        TurnPhase = TurnPhase.Resting;
        if (_mpSpawner.IsServer) {
            foreach (var b in _mpSpawner.Balls)
                b.TurnPhase = TurnPhase.Resting;

            TrickShotsSelector.Instance.ActivateButton(true);
            MPSpawner.Timer.Timer = TickTimer.CreateFromSeconds(_mpSpawner.Runner, MPSpawner.Timer.Seconds);
        }

        _shotMade = false;
        _incorrectShot = false;

        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }

    private void ResetScene() {
        if (_pauseRoutine != null) {
            StopCoroutine(_pauseRoutine);
            _pauseRoutine = null;
        }
        
        _mpSpawner.EndGame();

        _shotMade = false;
        _incorrectShot = false;
        TurnPhase = TurnPhase.Resting;
        
        _players.Clear();
        TrickShotsSelector.Instance.ClearTricks();
        TrickShotsSelector.Instance.CloseMenu();
        GameUiManager.Instance.UpdateScore(_players);
        GameUiManager.Instance.HideLobbyInfo();
        GameUiManager.Instance.ShowPracticeInfo(false);
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), MenuManager.Instance.BoomboxEnabled);
        
        _localBasketball.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        _localBoombox.ResetPosition(MenuManager.Instance.CurrentLevel.boomboxRespawnPoint);

        _cameraTransform.position = new Vector3(
            MenuManager.Instance.CurrentLevel.location.x,
            MenuManager.Instance.CurrentLevel.location.y,
            _cameraTransform.position.z);
    }

    public void NextPlayersTurn() {
        if (_incorrectShot) {
            _audioSource.PlayOneShot(_whistle);
            GameUiManager.Instance.ShowBanner("Wrong Shot!", 2f);
        } else if (_shotMade) {
            GameUiManager.Instance.ShowBanner("It's Good", 2f);
        } else
            GameUiManager.Instance.ShowBanner("No Good!", 2f);
        
        var successfulShot = _shotMade && !_incorrectShot;

        if (successfulShot && !Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).SetShot = true;
        else if (!successfulShot && Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).Score++;
        
        TrickShotsSelector.Instance.ActivateButton(false);
        TrickShotsSelector.Instance.CloseMenu();
        TurnPhase = TurnPhase.Transitioning;
        
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        GameUiManager.Instance.UpdateScore(_players);
        _localBasketball.CancelActions();

        _pauseRoutine = StartCoroutine(PauseSwitchingTurns());
    }
    
    public void NextMPPlayersTurn() {
        if (_transitioning) return;
        _transitioning = true;
        
        if (_incorrectShot) {
            _audioSource.PlayOneShot(_whistle);
            GameUiManager.Instance.ShowBanner("Wrong Shot!", 2f);
        } else if (_shotMade) {
            GameUiManager.Instance.ShowBanner("It's Good", 2f);
        } else
            GameUiManager.Instance.ShowBanner("No Good!", 2f);
        
        var players = MPBasketball.Players;

        if (_mpSpawner.IsServer) {
            var successfulShot = _shotMade && !_incorrectShot;
            if (successfulShot && !MPPlayer.SomeHasAShotSet(players))
                MPPlayer.CurrentPlayer(players).SetShot = true;
            else if (!successfulShot && MPPlayer.SomeHasAShotSet(players))
                MPPlayer.CurrentPlayer(players).Score++;
            
            foreach (var b in _mpSpawner.Balls)
                b.TurnPhase = TurnPhase.Transitioning;

            MPSpawner.Timer.Timer = TickTimer.None;
        }
        
        MPPlayer.CurrentPlayer(players).Basketball.CancelActions();
        TrickShotsSelector.Instance.ActivateButton(false);
        TrickShotsSelector.Instance.CloseMenu();
        
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        _pauseRoutine = StartCoroutine(PauseSwitchingMPTurns());
    }

    private IEnumerator PauseSwitchingTurns() {
        yield return new WaitForSeconds(2.5f);
        _shotMade = false;
        _incorrectShot = false;
        
        if (_players.Count(p => p.Lost) != _players.Count - 1) {
            if (!Player.SomeHasAShotSet(_players) || Player.PlayerWhoSetAShot(_players) == Player.NextPlayer(_players)) {
                Player.ClearAllShots(_players);
                TrickShotsSelector.Instance.ClearTricks();
                _localBasketball.ResetGravity();
                TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu && !Player.NextPlayer(_players).IsAi);
                TurnPhase = TurnPhase.Resting;
            } else {
                _localBasketball.ResetShotPosition();
                TrickShotsSelector.Instance.ActivateButton(false);
                TurnPhase = TurnPhase.Responding;
            }
        } else {
            TurnPhase = TurnPhase.Finished;
        }
        
        Player.GoToNextPlayer(_players);

        if (_players.Count(p => p.Lost) == _players.Count - 1) {
            GameUiManager.Instance.ShowBanner(Player.CurrentPlayer(_players).Name + " Wins!!", 12f);
            _audioSource.PlayOneShot(_applause);
            TrickShotsSelector.Instance.ActivateButton(false);
        } else {
            GameUiManager.Instance.ShowBanner(Player.CurrentPlayer(_players).Name + "'s Turn", 2f);
            _aiController.enabled = Player.CurrentPlayer(_players).IsAi;
            var c = _players.IndexOf(Player.CurrentPlayer(_players)) switch {
                0 => Color.red,
                1 => Color.green,
                2 => Color.yellow,
                3 => Color.cyan,
                _ => Color.HSVToRGB(0.07f, 1f, 1f)
            };
            _localBasketball.ChangeColor(c);
            GameUiManager.Instance.UpdateScore(_players);
            MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
        }
    }
    private IEnumerator PauseSwitchingMPTurns() {
        yield return new WaitForSeconds(2.5f);
        _transitioning = false;
        _shotMade = false;
        _incorrectShot = false;
        var players = MPBasketball.Players;
        
        if (players.Count(p => p.Lost) != _players.Count - 1) {
            if (!MPPlayer.SomeHasAShotSet(players) || MPPlayer.PlayerWhoSetAShot(players) == MPPlayer.NextPlayer(players)) {
                if (_mpSpawner.IsServer) {
                    MPPlayer.ClearAllShots(players);
                    MPPlayer.CurrentPlayer(players).Basketball.SwapPositionToReset(MPPlayer.NextPlayer(players).Basketball);
                    foreach (var b in _mpSpawner.Balls)
                        b.TurnPhase = TurnPhase.Resting;
                    
                    TrickShotsSelector.Instance.ActivateButton(
                        MPPlayer.NextPlayer(players).Basketball.IsMe && 
                        !MenuManager.InMenu && 
                        MPPlayer.NextPlayer(players).Basketball.TurnPhase is not TurnPhase.Responding
                    );
                }
                
                TrickShotsSelector.Instance.ClearTricks();
            } else {
                if (_mpSpawner.IsServer) {
                    MPPlayer.CurrentPlayer(players).Basketball
                        .SwapPositionToShot(MPPlayer.NextPlayer(players).Basketball);
                    foreach (var b in _mpSpawner.Balls)
                        b.TurnPhase = TurnPhase.Responding;
                    
                    TrickShotsSelector.Instance.ActivateButton(false);
                }
            }
        } else {
            if (_mpSpawner.IsServer)
                foreach (var b in _mpSpawner.Balls)
                    b.TurnPhase = TurnPhase.Finished;
        }
        
        if (_mpSpawner.IsServer)
            MPPlayer.GoToNextPlayer(players);

        var p = _mpSpawner.IsServer ? MPPlayer.CurrentPlayer(players) : MPPlayer.NextPlayer(players);
        
        if (_players.Count(p => p.Lost) == _players.Count - 1) {
            GameUiManager.Instance.ShowBanner(p.Name + " Wins!!", 12f);
            _audioSource.PlayOneShot(_applause);
            TrickShotsSelector.Instance.ActivateButton(false);
        } else {
            GameUiManager.Instance.ShowBanner(p.Name + "'s Turn", 2f);
            if (_mpSpawner.IsServer)
                MPSpawner.Timer.Timer = TickTimer.CreateFromSeconds(_mpSpawner.Runner, MPSpawner.Timer.Seconds);
        }
    }

    public void ShotMade() {        
        _shotMade = true;
        _incorrectShot = !TrickShotsSelector.Instance.AllAccomplished();
        
        if (Mode is GameType.Practice or GameType.OnlineLobby) {
            if (_incorrectShot) {
                _audioSource.PlayOneShot(_whistle);
                GameUiManager.Instance.ShowBanner("Wrong Shot!", 3f);
            } else if (_shotMade) {
                GameUiManager.Instance.ShowBanner("It's Good!", 3f);
            }
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else {
            if (Mode is GameType.OnlineMatch)
                NextMPPlayersTurn();
            else
                NextPlayersTurn();
        }
    }
    public void ShotMissed(GameObject go) {
        _shotMade = false;
        
        if (Mode is GameType.Practice or GameType.OnlineLobby) {
            if (go.TryGetComponent<BasketballFlick>(out var lb))
                lb.ResetGravity();
            else if (go.TryGetComponent<MPBasketball>(out var mb))
                mb.ResetGravity();
            TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu && Mode is GameType.Practice);
            TurnPhase = TurnPhase.Resting;
            if (_mpSpawner.IsServer)
                foreach (var b in _mpSpawner.Balls)
                    b.TurnPhase = TurnPhase.Resting;
            
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else {
            if (Mode is GameType.OnlineMatch)
                NextMPPlayersTurn();
            else
                NextPlayersTurn();
        }
    }
    
    public void OutOfBounds(GameObject go) {
        if (go.TryGetComponent<BasketballFlick>(out var lb))
            lb.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        else if (go.TryGetComponent<MPBasketball>(out var mb))
            mb.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        
        ShotMissed(go);
    }
    public void StartedShot() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), 
            (Mode is GameType.OnlineLobby or GameType.OnlineMatch && MPSpawner.Boombox) ? !MPSpawner.Boombox.Active : !MenuManager.Instance.BoomboxEnabled);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Wall"), false);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Net"), false);
        TurnPhase = TurnPhase.Charging;
        if (_mpSpawner.IsServer)
            foreach (var b in _mpSpawner.Balls)
                b.TurnPhase = TurnPhase.Charging;
        
        if (Mode is GameType.Practice or GameType.OnlineLobby)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        else
            TrickShotsSelector.Instance.ActivateButton(false);

        if (TrickShotsSelector.Instance.HasShot("Blindfolded")) {
            _blindfold.PutOn(true);
        }
    }

    public void EndedShot() {
        if (TrickShotsSelector.Instance.HasShot("Blindfolded"))
            _blindfold.PutOn(false);
        
        if (_mpSpawner.IsServer)
            foreach (var b in _mpSpawner.Balls)
                b.TurnPhase = TurnPhase.Shooting;
        TurnPhase = TurnPhase.Shooting;
    }

    public void StartedMove() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Wall"), _aiController.enabled);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Net"), _aiController.enabled);
        
        if (_mpSpawner.IsServer)
            foreach (var b in _mpSpawner.Balls)
                b.TurnPhase = TurnPhase.Moving;
        TurnPhase = TurnPhase.Moving;
        if (Mode is GameType.Practice or GameType.OnlineLobby)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }
}
