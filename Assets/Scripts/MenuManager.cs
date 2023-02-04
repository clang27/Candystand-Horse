using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public struct BasketballLevel {
    public Vector3 location, respawnPoint, goalPoint;
    public BasketballGoal goal;
    public TimerUi timer;
}
public class MenuManager : MonoBehaviour {
    public static MenuManager Instance;
    public static bool InMenu;
    
    [SerializeField] private GameObject _menu;
    [SerializeField] private TextMeshProUGUI _levelTextMesh, _playerCountTextMesh;
    private int _levelNumber = 0;
    private int _playerCount = 2;
    private const int MinPlayerCount = 2, MaxPlayerCount = 4;
    
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
                respawnPoint = lo.transform.GetChild(0).position,
                goalPoint = lo.GetComponentInChildren<BasketballGoal>().transform.GetChild(0).GetChild(0).position
            };
            _levels.Add(bl);
        }
        
        _cameraTransform = FindObjectOfType<Camera>().transform;
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
    
    public void ToggleMenu() {
        InMenu = !InMenu;
        _menu.SetActive(InMenu);
    }

    private void CloseMenu() {
        InMenu = false;
        _menu.SetActive(InMenu);
    }

    public void GoToLobby() {
        CloseMenu();
        
        GameManager.Instance.GoToLobby();
    }

    public void GoToLocal() {
        CloseMenu();
        
        GameManager.Instance.StartNewGame(_playerCount);
    }
}
