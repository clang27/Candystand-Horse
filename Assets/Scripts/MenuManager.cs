using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public struct BasketballLevel {
    public Vector3 location, ballRespawnPoint, boomboxRespawnPoint, goalPoint;
    public BasketballGoal goal;
    public TimerUi timer;
}

[Serializable]
public enum GameType {
    Local, Ai, OnlineLobby, Practice, OnlineMatch
}

public class MenuManager : MonoBehaviour {
    public static MenuManager Instance;
    public static bool InMenu;
    
    [SerializeField] private GameObject _menu;
    [SerializeField] private TextMeshProUGUI _levelTextMesh, _playerCountTextMesh, _shotClockTextMesh;
    private CanvasGroup _submenuOne, _submenuTwo, _multimenu, _joinmenu;
    
    private int _levelNumber = 0;
    private int _playerCount = 2;
    public int ShotClock { get; private set; } = 15;

    private bool _boomboxEnabled;
    public bool BoomboxEnabled => _boomboxEnabled && GameManager.Instance.Mode != GameType.Ai;
    private GameType BufferedMode { get; set; }
    private bool BufferedHost { get; set; }
    
    private const int MinPlayerCount = 2, MaxPlayerCount = 4;
    private const int MinShotClock = 10, MaxShotClock = 30;
    
    public BasketballLevel CurrentLevel => _levels[_levelNumber];
    private List<BasketballLevel> _levels = new();

    private Transform _cameraTransform;
    private void Awake() {
        Instance = this;
        var levelObjects = GameObject.FindGameObjectsWithTag("Level").OrderBy(go => go.name);
        foreach (var lo in levelObjects) {
            var bl = new BasketballLevel {
                goal = lo.GetComponentInChildren<BasketballGoal>(),
                location = lo.transform.position,
                timer = lo.GetComponentInChildren<TimerUi>(),
                ballRespawnPoint = lo.transform.GetChild(0).position,
                boomboxRespawnPoint = lo.transform.GetChild(1).position,
                goalPoint = lo.GetComponentInChildren<BasketballGoal>().transform.GetChild(0).GetChild(0).position
            };
            _levels.Add(bl);
        }
        
        _cameraTransform = FindObjectOfType<Camera>().transform;
        _submenuOne = _menu.transform.GetChild(0).GetComponent<CanvasGroup>();
        _submenuTwo = _menu.transform.GetChild(1).GetComponent<CanvasGroup>();
        _multimenu = _menu.transform.GetChild(2).GetComponent<CanvasGroup>();
        _joinmenu = _menu.transform.GetChild(3).GetComponent<CanvasGroup>();
    }

    private void EnableMenu(CanvasGroup cg, bool b) {
        cg.alpha = (b) ? 1f : 0f;
        cg.interactable = b;
        cg.blocksRaycasts = b;
    }

    private void SelectLevel(int levelNumber) {
        _levelNumber = levelNumber;
        
        _cameraTransform.position = new Vector3(
            CurrentLevel.location.x,
            CurrentLevel.location.y,
            _cameraTransform.position.z);
    }

    public void IncreaseLevel(int amount) {
        var nextLevel = _levelNumber + amount;
        if (nextLevel >= _levels.Count)
            nextLevel = 0;
        else if (nextLevel < 0)
            nextLevel = _levels.Count - 1;

        _levelTextMesh.text = (nextLevel+1).ToString();
        SelectLevel(nextLevel);
    }
    
    public void IncreasePlayerCount(int amount) {
        var nextPlayerCount = _playerCount + amount;
        if (nextPlayerCount > MaxPlayerCount)
            nextPlayerCount = MinPlayerCount;
        else if (nextPlayerCount < MinPlayerCount)
            nextPlayerCount = MaxPlayerCount;

        _playerCountTextMesh.text = nextPlayerCount.ToString();
        _playerCount = nextPlayerCount;
    }
    
    public void IncreaseShotClock(int amount) {
        var nextClock = ShotClock + amount;
        if (nextClock > MaxShotClock)
            nextClock = MinShotClock;
        else if (nextClock < MinShotClock)
            nextClock = MaxShotClock;

        _shotClockTextMesh.text = nextClock.ToString();
        ShotClock = nextClock;
    }
    public void TurnOnBoombox(bool b) {
        _boomboxEnabled = b;
    }
    
    public void ToggleMenu() {
        InMenu = !InMenu;
        
        if (TrickShotsSelector.InMenu)
            TrickShotsSelector.Instance.CloseMenu();
        
        TrickShotsSelector.Instance.ActivateButton(!InMenu && GameManager.Instance.TurnPhase is not TurnPhase.Shooting);
        Back();
        _menu.SetActive(InMenu);
    }

    private void CloseMenu() {
        InMenu = false;
        
        Back();
        _menu.SetActive(false);
    }

    public void SelectMode(int mode) {
        BufferedMode = (GameType)mode;

        foreach(var item in _menu.GetComponentsInChildren<MenuLine>())
            item.ActivateIfRightMode(BufferedMode);
        
        EnableMenu(_submenuOne, false);
        EnableMenu(_submenuTwo, BufferedMode != GameType.OnlineLobby);
        EnableMenu(_multimenu, BufferedMode == GameType.OnlineLobby);
    }

    public void Host() {
        BufferedHost = true;
        EnableMenu(_multimenu, false);
        EnableMenu(_submenuTwo, true);
    }

    public void Join() {
        BufferedHost = false;
        EnableMenu(_multimenu, false);
        EnableMenu(_joinmenu, true);
    }
    
    public void Back() {
        EnableMenu(_submenuOne, true);
        EnableMenu(_submenuTwo, false);
        EnableMenu(_multimenu, false);
        EnableMenu(_joinmenu, false);
    }

    public void Play() {
        CloseMenu();
        TrickShotsSelector.Instance.ActivateButton(true);
        
        switch (BufferedMode) {
            case GameType.Practice:
                GameManager.Instance.GoToPractice();
                break;
            case GameType.Local:
                GameManager.Instance.GoToLocal(_playerCount);
                break;
            case GameType.OnlineLobby:
                GameManager.Instance.GoToOnline(BufferedHost);
                break;
            case GameType.Ai:
                GameManager.Instance.GoToAi();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
