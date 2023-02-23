using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickShot: MonoBehaviour {
    [SerializeField] public int TrickNumber;
    public string Name { get; private set; }
    private bool Selected { get; set; }
    public List<GameObject> ShotObjects;
    public int TargetOccurrences = 0;
    public bool ExactTarget;
    [SerializeField] private List<TrickShot> conflictingShots;
    public List<IShot> Shots => ShotObjects.Select(obj => obj.GetComponent<IShot>()).ToList();
    
    private Button _button;
    private Image _checkmark;
    private void Awake() {
        _button = GetComponent<Button>();
        _checkmark = GetComponentInChildren<Image>();
    }

    private void Start() {
        Name = GetComponentInChildren<TextMeshProUGUI>().text;
        _button.onClick.AddListener(SelectShot);
    }
    public void SelectShot() {
        if (MPSpawner.Tricks)
            MPSpawner.TricksClicked[TrickNumber - 1] = true;
        else
            Select();
    }
    private void Select() {
        if (!Selected) {
            ShowCheckmark(true);
            TrickShotsSelector.Instance.AddShot(this);
            foreach (var shot in conflictingShots) {
                shot.ShowCheckmark(false);
                TrickShotsSelector.Instance.RemoveShotIfExists(shot);
            }
        } else {
            ShowCheckmark(false);
            TrickShotsSelector.Instance.RemoveShotIfExists(this);
        }
    }
    
    public static void SelectTrickShotWithInput(int tn) {
        var ts = FindObjectsOfType<TrickShot>().First(t => t.TrickNumber == tn);

        ts.Select();
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