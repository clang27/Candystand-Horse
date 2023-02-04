using UnityEngine;

public class Backboard : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private BasketballSounds _sounds;
    private void Awake() {
        _sounds = GetComponent<BasketballSounds>();
    }
    private void OnCollisionEnter2D(Collision2D col) {
        if (col.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
            _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
        if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;
        
        CurrentOccurrences++;
    }
}
