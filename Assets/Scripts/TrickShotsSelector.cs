using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TrickShotsSelector : MonoBehaviour {
    public static bool InMenu;
    
    [SerializeField] private GameObject _trickShotMenu;
    [SerializeField] private TextMeshProUGUI _trickList;
    private List<TrickShot> Tricks { get; set; }

    private void Awake() {
        Tricks = new List<TrickShot>();
    }

    public void ToggleMenu() {
        if (GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Resting) return;
        InMenu = !InMenu;
        _trickShotMenu.SetActive(InMenu);
    }

    public void AddShot(TrickShot shot) {
        Tricks.Add(shot);
        UpdateListText();
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
        if (!HasShot(shot2)) return;
        
        Tricks.Remove(Tricks.First(shot1 => shot1.Name.Equals(shot2.Name)));
        UpdateListText();
    }

    public void ClearTricks() {
        Tricks.Clear();
        UpdateListText();
    }

    public bool HasShot(TrickShot shot2) {
        return Tricks.Any(shot1 => shot1.Name.Equals(shot2.Name));
    }
    
    public bool AllAccomplished() {
        return Tricks.Count == 0 || Tricks.All(trick => 
            trick.ExactTarget ? 
            trick.Shots.Sum(s => s.CurrentOccurrences) == trick.TargetOccurrences:
            trick.Shots.Sum(s => s.CurrentOccurrences) >= trick.TargetOccurrences
        );
    }
}
