using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IShot {
    public int CurrentOccurrences { get; set; }
}

public class BasketballGoal : MonoBehaviour {
    private List<IShot> _trickShots = new();
    private BasketballSounds _swishSounds;
    private bool _goalStarted, _shotMade;

    private void Awake() {
        _swishSounds = GetComponent<BasketballSounds>();
        _trickShots = FindObjectsOfType<Transform>()
            .Select(obj => obj.GetComponent<IShot>())
            .Where(obj => obj != null)
            .ToList();
    }

    public void StartGoal() {
        if (_shotMade) return;
        
        _goalStarted = true;
    }
    
    public void EndGoal(float speedOfEntry) {
        if (_goalStarted) {
            _swishSounds.PlaySound(speedOfEntry);
            GameManager.Instance.ShotMade();
        }
        _goalStarted = false;
    }

    public void ResetGoal() {
        foreach (var shot in _trickShots) {
            shot.CurrentOccurrences = 0;
        }
        _goalStarted = false;
        _shotMade = false;
    }
}
