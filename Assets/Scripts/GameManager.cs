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
    
    private void Awake() {
        Instance = this;

        _audioSource = GetComponent<AudioSource>();
        _localBasketball = FindObjectOfType<BasketballFlick>();
        _aiController = _localBasketball.GetComponent<AiController>();
        _localBoombox = FindObjectOfType<Boombox>();
        _blindfold = FindObjectOfType<Blindfold>();
        _cameraTransform = FindObjectOfType<Camera>().transform;
    }

    private void Start() {
        GoToPractice();
    }
    public void GoToPractice() {
        Mode = GameType.Practice;
        ResetScene();
        _localBasketball.ChangeColor(Color.HSVToRGB(0.07f, 1f, 1f));
    }

    public void GoToLocal(int playerCount) {
        Mode = GameType.Local;
        ResetScene();
        _localBasketball.ChangeColor(Color.red);
        
        for (var i = 0; i < playerCount; i++)
            _players.Add(new Player("P" + (i + 1), playerCount));
        
        
        _players[0].IsTurn = true;
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }

    public void GoToAi() {
        Mode = GameType.Ai;
        ResetScene();
        _localBasketball.ChangeColor(Color.red);
        
        _players.Add(new Player("P1", 2));
        _players.Add(new Player("AI", 2));
        
        _players[0].IsTurn = true;
        _players[1].IsAi = true;
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }
    public void GoToOnline() {
        Mode = GameType.Online;
        ResetScene();
        _localBasketball.gameObject.SetActive(false);
        _localBoombox.gameObject.SetActive(false);
    }

    private void ResetScene() {
        if (_pauseRoutine != null) {
            StopCoroutine(_pauseRoutine);
            _pauseRoutine = null;
        }
        
        _shotMade = false;
        _incorrectShot = false;
        TurnPhase = TurnPhase.Resting;
        
        _players.Clear();
        TrickShotsSelector.Instance.ClearTricks();
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), MenuManager.Instance.BoomboxEnabled);
        
        _localBasketball.gameObject.SetActive(true);
        _localBoombox.gameObject.SetActive(true);
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
        
        var successfulShot = _shotMade && FindObjectOfType<TrickShotsSelector>().AllAccomplished();

        if (successfulShot && !Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).SetShot = true;
        else if (!successfulShot && Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).Score++;
        
        TrickShotsSelector.Instance.ActivateButton(false);
        TrickShotsSelector.Instance.CloseMenu();
        TurnPhase = TurnPhase.Transitioning;
        _localBasketball.CancelActions();
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();

        _pauseRoutine = StartCoroutine(PauseSwitchingTurns());
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
                TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu);
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
        } else {
            GameUiManager.Instance.ShowBanner(Player.CurrentPlayer(_players).Name + "'s Turn", 2f);
            _aiController.enabled = Player.CurrentPlayer(_players).IsAi;
            switch (_players.IndexOf(Player.CurrentPlayer(_players))) {
                case 0: _localBasketball.ChangeColor(Color.red); break;
                case 1: _localBasketball.ChangeColor(Color.green); break;
                case 2: _localBasketball.ChangeColor(Color.yellow); break;
                case 3: _localBasketball.ChangeColor(Color.cyan); break;
            }
            GameUiManager.Instance.UpdateScore(_players);
            MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
        }
    }

    public void ShotMade() {
        _shotMade = true;
        _incorrectShot = !TrickShotsSelector.Instance.AllAccomplished();
        
        if (Mode == GameType.Practice) {
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
        
        if (Mode == GameType.Practice) {
            _localBasketball.ResetGravity();
            TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu);
            TurnPhase = TurnPhase.Resting;
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else {
            NextPlayersTurn();
        }
    }
    
    public void OutOfBounds() {
        _localBasketball.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);
        ShotMissed();
    }
    public void StartedShot() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), !MenuManager.Instance.BoomboxEnabled);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Wall"), false);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Net"), false);
        TurnPhase = TurnPhase.Charging;
        
        if (Mode == GameType.Practice)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        else
            TrickShotsSelector.Instance.ActivateButton(false);
        
        if (TrickShotsSelector.Instance.HasShot("Blindfolded"))
            _blindfold.PutOn(true);
    }

    public void EndedShot() {
        if (TrickShotsSelector.Instance.HasShot("Blindfolded"))
            _blindfold.PutOn(false);
        
        TurnPhase = TurnPhase.Shooting;
    }

    public void StartedMove() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Wall"), _aiController.enabled);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Net"), _aiController.enabled);
        
        TurnPhase = TurnPhase.Moving;
        if (Mode == GameType.Practice)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }
}
