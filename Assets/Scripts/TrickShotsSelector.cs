using System.Collections.Generic;
using System.Linq;
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
    private List<TrickShot> Tricks { get; set; }

    private void Awake() {
        Instance = this;
        Tricks = new List<TrickShot>();
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
        Tricks.Add(shot);
        UpdateListText();
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

    public void RemoveShotIfExists(TrickShot shot2) {
        if (!HasShot(shot2.Name)) return;
        
        Tricks.Remove(Tricks.First(shot1 => shot1.Name.Equals(shot2.Name)));
        UpdateListText();
    }

    public void ClearTricks() {
        Tricks.ForEach(t => t.ClearCheckmark());
        Tricks.Clear();
        UpdateListText();
    }

    public bool HasShot(string n) {
        return Tricks.Any(shot => shot.Name.Equals(n));
    }
    
    public bool AllAccomplished() {
        return Tricks.Count == 0 || Tricks.All(trick => 
            trick.ExactTarget ? 
            trick.Shots.Sum(s => s.CurrentOccurrences) == trick.TargetOccurrences:
            trick.Shots.Sum(s => s.CurrentOccurrences) >= trick.TargetOccurrences
        );
    }
}
