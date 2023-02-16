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
    private MPSpawner _mpSpawner;
    
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
        _localBasketball.ChangeColor(Color.HSVToRGB(0.07f, 1f, 1f));
    }

    public void GoToLocal(int playerCount) {
        Mode = GameType.Local;
        ResetScene();
        
        for (var i = 0; i < playerCount; i++)
            _players.Add(new Player("P" + (i + 1), playerCount));
        
        
        _players[0].IsTurn = true;
        _localBasketball.ChangeColor(_players[0].Color);
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }

    public void GoToAi() {
        Mode = GameType.Ai;
        ResetScene();
        
        _players.Add(new Player("P1", 2));
        _players.Add(new Player("AI", 2));
        
        _players[0].IsTurn = true;
        _players[1].IsAi = true;
        _localBasketball.ChangeColor(_players[0].Color);
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
    }
    public void GoToOnline(bool host) {
        Mode = GameType.OnlineLobby;
        ResetScene();
        _localBasketball.gameObject.SetActive(false);
        _localBoombox.gameObject.SetActive(false);
        
        _mpSpawner.StartGame(host);
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
        _mpSpawner.EndGame();

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
            _localBasketball.ChangeColor(Player.CurrentPlayer(_players).Color);
            GameUiManager.Instance.UpdateScore(_players);
            MenuManager.Instance.CurrentLevel.timer.StartCountdown(MenuManager.Instance.ShotClock);
        }
    }

    public void ShotMade() {
        _shotMade = true;
        _incorrectShot = !TrickShotsSelector.Instance.AllAccomplished();
        
        if (Mode is GameType.Practice or GameType.OnlineLobby) {
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
    public void ShotMissed(GameObject go) {
        _shotMade = false;
        
        if (Mode is GameType.Practice or GameType.OnlineLobby) {
            if (go.TryGetComponent<BasketballFlick>(out var lb))
                lb.ResetGravity();
            else if (go.TryGetComponent<MPBasketball>(out var mb))
                mb.ResetGravity();
            TrickShotsSelector.Instance.ActivateButton(!MenuManager.InMenu && Mode is GameType.Practice);
            TurnPhase = TurnPhase.Resting;
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        } else if (Mode is not GameType.OnlineLobby){
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
