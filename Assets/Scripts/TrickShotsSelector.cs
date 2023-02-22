using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickShotsSelector : MonoBehaviour {
    public static TrickShotsSelector Instance;
    public static bool InMenu { get; private set; }
    
    [SerializeField] private GameObject _trickShotMenu;
    [SerializeField] private TextMeshProUGUI _trickList;
    [SerializeField] private float _startY, _endY;
    [SerializeField] private Button _trickShotButton;
    [SerializeField] private Sprite _upArrow, _downArrow;
    private Image _trickShotButtonArrow;
    private List<TrickShot> Tricks {get; } = new();

    private void Awake() {
        Instance = this;
        _trickShotButtonArrow = _trickShotButton.transform.GetChild(0).GetComponent<Image>();
    }
    
    public void ToggleMenu() {
        InMenu = !InMenu;
        _trickShotButtonArrow.sprite = (InMenu) ? _downArrow : _upArrow;
        LeanTween.cancel(_trickShotMenu.GetComponent<RectTransform>());
        LeanTween
            .moveY(_trickShotMenu.GetComponent<RectTransform>(), (InMenu) ? _endY : _startY, 0.3f)
            .setEase(LeanTweenType.easeInSine);
    }

    public void CloseMenu() {
        if (InMenu)
            ToggleMenu();
    }

    public void AddShot(TrickShot shot) {
        var b = MPSpawner.Ball;
        if (b) {
            for (var i = 0; i < b.ClientTricks.Length; i++) {
                if (b.ClientTricks[i].Name.Length >= 1) continue;
                
                b.ClientTricks[i] = new MPTrickShot {
                    Name = shot.Name,
                    TargetOccurrences = shot.TargetOccurrences,
                    ExactTarget = shot.ExactTarget,
                    Occurrences = 0
                };
                break;
            }
        } else {
            Tricks.Add(shot);
            UpdateListText();
        }
    }
    public void ActivateButton(bool b) {
        _trickShotButton.interactable = b;
        _trickShotButtonArrow.color = new Color(1f, 1f, 1f, (b) ? 0.8f: 0f);
    }

    private void UpdateListText() {
        if (Tricks.Count == 0) {
            _trickList.text = "";
            return;
        }
        var t = Tricks.Aggregate("", (current, trick) => current + (trick.Name + ", "));
        t = t.Remove(t.Length - 2, 2);
        _trickList.text = t;
    }
    
    public void UpdateMPListText(MPTrickShot[] tricks) {
        var s = "";
        for (var i = 0; i < tricks.Length; i++) {
            if (tricks[i].Name.Length < 1) continue;

            s += tricks[i].Name + ", ";
        }
        
        if (s.Length > 2)
            s = s.Remove(s.Length - 2, 2);
        _trickList.text = s;
    }

    public void RemoveShotIfExists(TrickShot shot2) {
        var b = MPSpawner.Ball;
        if (b) {
            for (var i = 0; i < b.ClientTricks.Length; i++) {
                if (b.ClientTricks[i].Name.Equals(shot2.Name))
                    b.ClientTricks[i] = default;
            }
        } else {
            Tricks.Remove(Tricks.First(shot1 => shot1.Name.Equals(shot2.Name)));
            UpdateListText();
        }
    }

    public void ClearTricks() {
        foreach (var t in FindObjectsOfType<TrickShot>())
            t.ClearCheckmark();
        
        var b = MPSpawner.Ball;
        if (b) {
            for (var i = 0; i < b.ClientTricks.Length; i++) {
                MPBasketball.ServerTricks[i] = default;
                b.ClientTricks[i] = default;
            }
        } else {
            Tricks.Clear();
            UpdateListText();
        }
    }

    public bool HasShot(string n) {
        var b = MPSpawner.Ball;
        if (b)
            return b.ClientTricks.Any(shot => shot.Name.Equals(n));
        
        return Tricks.Any(shot => shot.Name.Equals(n));
    }
    
    public bool AllAccomplished() {
        if (MPSpawner.Ball)
            return MPBasketball.ServerTricks
                .Where(trick => trick.Name.Length > 1)
                .All(trick => trick.ExactTarget ? 
                    trick.Occurrences == trick.TargetOccurrences : 
                    trick.Occurrences >= trick.TargetOccurrences
                );
            
        return Tricks.Count == 0 || Tricks.All(trick => 
            trick.ExactTarget ? 
            trick.Shots.Sum(s => s.CurrentOccurrences) == trick.TargetOccurrences:
            trick.Shots.Sum(s => s.CurrentOccurrences) >= trick.TargetOccurrences
        );
    }
}
