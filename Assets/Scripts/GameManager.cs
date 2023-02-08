using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase {
    Moving, Responding, Charging, Shooting, Resting, Transitioning
}

public class GameManager : MonoBehaviour {
    public int ShotClock = 15;
    public bool BoomboxEnabled = false;
    
    public static GameManager Instance;
    public TurnPhase TurnPhase { get; set; }
    private BasketballFlick _basketball;
    private TrickShotsSelector _trickShotsSelector;
    private Transform _cameraTransform;
    private List<Player> _players = new();
    private bool _shotMade { get; set; }
    public bool InLobby { get; private set; }
    
    private void Awake() {
        Instance = this;

        _basketball = FindObjectOfType<BasketballFlick>();
        _trickShotsSelector = FindObjectOfType<TrickShotsSelector>();
        _cameraTransform = FindObjectOfType<Camera>().transform;
    }

    private void Start() {
        GoToLobby();
    }

    public void GoToLobby() {
        ResetScene(true, Color.HSVToRGB(0.07f, 1f, 1f));
    }

    public void StartNewGame(int playerCount) {
        ResetScene(false, Color.red);
        
        for (var i = 0; i < playerCount; i++)
            _players.Add(new Player("P" + (i + 1)));
        
        
        _players[0].IsTurn = true;
        
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(ShotClock);
    }

    private void ResetScene(bool inLobby, Color ballColor) {
        _shotMade = false;
        TurnPhase = TurnPhase.Resting;
        InLobby = inLobby;
        
        _players.Clear();
        _trickShotsSelector.ClearTricks();
        GameUiManager.Instance.ShowShotsButton(!InLobby);
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), BoomboxEnabled);
        _basketball.ResetPosition(MenuManager.Instance.CurrentLevel.respawnPoint);
        _basketball.ChangeColor(ballColor);
        
        _cameraTransform.position = new Vector3(
            MenuManager.Instance.CurrentLevel.location.x,
            MenuManager.Instance.CurrentLevel.location.y,
            _cameraTransform.position.z);
    }
    
    public void NextPlayersTurn() {
        var successfulShot = _shotMade && FindObjectOfType<TrickShotsSelector>().AllAccomplished();

        if (successfulShot && !Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).SetShot = true;
        else if (!successfulShot && Player.SomeHasAShotSet(_players))
            Player.CurrentPlayer(_players).Score++;

        TurnPhase = TurnPhase.Transitioning;
        _basketball.CancelActions();
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.EndCountdown();
        MenuManager.Instance.CurrentLevel.goal.ResetGoal();
        
        if (!_shotMade)
            GameUiManager.Instance.ShowShotBanner("No Shot");
        StartCoroutine(PauseSwitchingTurns());
    }

    private IEnumerator PauseSwitchingTurns() {
        yield return new WaitForSeconds(2f);
        if (!Player.SomeHasAShotSet(_players) || Player.PlayerWhoSetAShot(_players) == Player.NextPlayer(_players)) {
            Player.ClearAllShots(_players);
            _trickShotsSelector.ClearTricks();
            _basketball.ResetGravity();
            TurnPhase = TurnPhase.Resting;
        } else {
            _basketball.ResetShotPosition();
            TurnPhase = TurnPhase.Responding;
        }
        
        _shotMade = false;
        Player.GoToNextPlayer(_players);
        switch (_players.IndexOf(Player.CurrentPlayer(_players))) {
            case 0: _basketball.ChangeColor(Color.red); break;
            case 1: _basketball.ChangeColor(Color.green); break;
            case 2: _basketball.ChangeColor(Color.yellow); break;
            case 3: _basketball.ChangeColor(Color.cyan); break;
        }
        GameUiManager.Instance.UpdateScore(_players);
        MenuManager.Instance.CurrentLevel.timer.StartCountdown(ShotClock);
    }

    public void ShotMade() {
        if (_trickShotsSelector.AllAccomplished()) {
            _shotMade = true;
            GameUiManager.Instance.ShowShotBanner(!Player.SomeHasAShotSet(_players) && !InLobby ? "Shot Set" : "Nice Shot");
            if (!InLobby)
                NextPlayersTurn();
        } else 
            GameUiManager.Instance.ShowShotBanner("Incorrect Shot");
    }
    public void OutOfBounds() {
        _basketball.ResetPosition(MenuManager.Instance.CurrentLevel.respawnPoint);
        
        if (!InLobby && TurnPhase is TurnPhase.Shooting)
            NextPlayersTurn();
    }
}
