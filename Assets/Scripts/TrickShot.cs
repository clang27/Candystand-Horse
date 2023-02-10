using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickShot: MonoBehaviour {
    public string Name { get; private set; }
    private bool Selected { get; set; }
    public List<GameObject> ShotObjects;
    public int TargetOccurrences = 0;
    public bool ExactTarget;
    [SerializeField] private List<TrickShot> conflictingShots;
    public List<IShot> Shots => ShotObjects.Select(obj => obj.GetComponent<IShot>()).ToList();
    
    private Button _button;
    private TrickShotsSelector _trickShotsSelector;
    private Image _checkmark;
    private void Awake() {
        _button = GetComponent<Button>();
        _trickShotsSelector = FindObjectOfType<TrickShotsSelector>();
        _checkmark = GetComponentInChildren<Image>();
    }

    private void Start() {
        Name = GetComponentInChildren<TextMeshProUGUI>().text;
        _button.onClick.AddListener(delegate {
            if (!Selected) {
                ShowCheckmark(true);
                _trickShotsSelector.AddShot(this);
                foreach (var shot in conflictingShots) {
                    shot.ShowCheckmark(false);
                    _trickShotsSelector.RemoveShotIfExists(shot);
                }
            } else {
                ShowCheckmark(false);
                _trickShotsSelector.RemoveShotIfExists(this);
            }
        });
    }
    public void ClearCheckmark() {
        ShowCheckmark(false);
    }

    private void ShowCheckmark(bool b) {
        var c = _checkmark.color;
        _checkmark.color = new Color(c.r, c.g, c.b, (b) ? 1f : 0f);
        Selected = b;
    }
}