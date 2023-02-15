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
    Local, Ai, Online, Practice
}

public class MenuManager : MonoBehaviour {
    public static MenuManager Instance;
    public static bool InMenu;
    
    [SerializeField] private GameObject _menu;
    [SerializeField] private TextMeshProUGUI _levelTextMesh, _playerCountTextMesh, _shotClockTextMesh;
    private GameObject _submenuOne, _submenuTwo;
    
    private int _levelNumber = 0;
    private int _playerCount = 2;
    public int ShotClock { get; private set; } = 15;

    private bool _boomboxEnabled;
    public bool BoomboxEnabled => _boomboxEnabled && GameManager.Instance.Mode != GameType.Ai;
    private GameType BufferedMode { get; set; }
    
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
        _submenuOne = _menu.transform.GetChild(0).gameObject;
        _submenuTwo = _menu.transform.GetChild(1).gameObject;
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
        _submenuOne.SetActive(true);
        _submenuTwo.SetActive(false);
        _menu.SetActive(InMenu);
    }

    private void CloseMenu() {
        InMenu = false;
        
        _submenuOne.SetActive(true);
        _submenuTwo.SetActive(false);
        _menu.SetActive(false);
    }

    public void SelectMode(int mode) {
        BufferedMode = (GameType)mode;
        
        _submenuOne.SetActive(false);
        _submenuTwo.SetActive(true);
        
        foreach(var item in _submenuTwo.GetComponentsInChildren<MenuLine>())
            item.ActivateIfRightMode(BufferedMode);
    }
    public void Back() {
        _submenuOne.SetActive(true);
        _submenuTwo.SetActive(false);
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
            case GameType.Online:
                break;
            case GameType.Ai:
                GameManager.Instance.GoToAi();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
