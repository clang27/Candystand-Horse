using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnPhase {
    Moving, Responding, Charging, Shooting, Resting, Transitioning, Finished
}

public class GameManager : MonoBehaviour {
    [SerializeField] private AudioClip _applause, _whistle;
    public static GameManager Instance;
    public TurnPhase TurnPhase { get; set; }
    private BasketballFlick _basketball;
    private Boombox _boombox;
    private Transform _cameraTransform;
    private AudioSource _audioSource;
    private Blindfold _blindfold;
    private List<Player> _players = new();
    private bool _shotMade { get; set; }
    private bool _incorrectShot { get; set; }
    public GameMode Mode { get; private set; }
    
    private void Awake() {
        Instance = this;

        _audioSource = GetComponent<AudioSource>();
        _basketball = FindObjectOfType<BasketballFlick>();
        _boombox = FindObjectOfType<Boombox>();
        _blindfold = FindObjectOfType<Blindfold>();
        _cameraTransform = FindObjectOfType<Camera>().transform;
    }

    private void Start() {
        GoToPractice();
    }
    public void GoToPractice() {
        Mode = GameMode.Practice;
        ResetScene(Color.HSVToRGB(0.07f, 1f, 1f));
    }

    public void GoToLocal(int playerCount) {
        Mode = GameMode.Local;
        ResetScene(Color.red);
        
        for (var i = 0; i < playerCount; i++)
            _players.Add(new Player("P" + (i + 1), playerCount));
        
        
        _players[0].IsTurn = true;
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }

    private void ResetScene(Color ballColor) {
        _shotMade = false;
        _incorrectShot = false;
        TurnPhase = TurnPhase.Resting;
        
        _players.Clear();
        TrickShotsSelector.Instance.ClearTricks();
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), MenuManager.Instance.BoomboxEnabled);
        _basketball.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        _boombox.ResetPosition(MenuManager.Instance.CurrentLevel.boomboxRespawnPoint);
        _basketball.ChangeColor(ballColor);
        
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
        
        var successfulShot = _shotMade && FindObjectOfType<TrickShotsSelector>().AllAccomplished();

        if (successfulShot && !Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).SetShot = true;
        else if (!successfulShot && Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).Score++;
        
        TrickShotsSelector.Instance.ActivateButton(false);
        TrickShotsSelector.Instance.CloseMenu();
        TurnPhase = TurnPhase.Transitioning;
        _basketball.CancelActions();
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();

        StartCoroutine(PauseSwitchingTurns());
    }

    private IEnumerator PauseSwitchingTurns() {
        yield return new WaitForSeconds(2.5f);
        _shotMade = false;
        _incorrectShot = false;
        
        if (_players.Count(p => p.Lost) != _players.Count - 1) {
            if (!Player.SomeHasAShotSet(_players) || Player.PlayerWhoSetAShot(_players) == Player.NextPlayer(_players)) {
                Player.ClearAllShots(_players);
                TrickShotsSelector.Instance.ClearTricks();
                _basketball.ResetGravity();
                TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu);
                TurnPhase = TurnPhase.Resting;
            } else {
                _basketball.ResetShotPosition();
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
        } else {
            GameUiManager.Instance.ShowBanner(Player.CurrentPlayer(_players).Name + "'s Turn", 2f);
            switch (_players.IndexOf(Player.CurrentPlayer(_players))) {
                case 0: _basketball.ChangeColor(Color.red); break;
                case 1: _basketball.ChangeColor(Color.green); break;
                case 2: _basketball.ChangeColor(Color.yellow); break;
                case 3: _basketball.ChangeColor(Color.cyan); break;
            }
            GameUiManager.Instance.UpdateScore(_players);
            MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
        }
    }

    public void ShotMade() {
        _shotMade = true;
        _incorrectShot = !TrickShotsSelector.Instance.AllAccomplished();
        
        if (Mode == GameMode.Practice) {
            if (_incorrectShot) {
                _audioSource.Play();
                GameUiManager.Instance.ShowBanner("Wrong Shot!", 3f);
            } else if (_shotMade) {
                GameUiManager.Instance.ShowBanner("It's Good!", 3f);
            }
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else {
            NextPlayersTurn();
        }
    }
    public void ShotMissed() {
        _shotMade = false;
        
        if (Mode == GameMode.Practice) {
            _basketball.ResetGravity();
            TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu);
            TurnPhase = TurnPhase.Resting;
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else {
            NextPlayersTurn();
        }
    }
    
    public void OutOfBounds() {
        _basketball.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        ShotMissed();
    }
    public void StartedShot() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), !MenuManager.Instance.BoomboxEnabled);
        TurnPhase = TurnPhase.Charging;
        
        if (Mode == GameMode.Practice)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        else
            TrickShotsSelector.Instance.ActivateButton(false);
        
        if (TrickShotsSelector.Instance.BlindfoldIsOn())
            _blindfold.PutOn(true);
    }

    public void EndedShot() {
        if (TrickShotsSelector.Instance.BlindfoldIsOn())
            _blindfold.PutOn(false);
        
        TurnPhase = TurnPhase.Shooting;
    }

    public void StartedMove() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), true);
        TurnPhase = TurnPhase.Moving;
        if (Mode == GameMode.Practice)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }
}
