using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO: Remove this if going with input sent
public struct MPTrickShot : INetworkStruct {
    public NetworkString<_32> Name;
    public int TargetOccurrences;
    public int Occurrences;
    public NetworkBool ExactTarget;
}

public class TrickShot: MonoBehaviour {
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
    public void ClearCheckmark() {
        ShowCheckmark(false);
    }

    private void ShowCheckmark(bool b) {
        var c = _checkmark.color;
        _checkmark.color = new Color(c.r, c.g, c.b, (b) ? 1f : 0f);
        Selected = b;
    }
}