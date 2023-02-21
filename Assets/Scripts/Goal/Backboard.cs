using UnityEngine;

public class Backboard : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private BasketballSounds _sounds;
    private void Awake() {
        _sounds = GetComponent<BasketballSounds>();
    }
    private void OnCollisionEnter2D(Collision2D col) {
        if (!col.gameObject.name.Contains("Ball")) return;
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = col.gameObject.GetComponent<MPBasketball>();
            if (ball.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
                _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
            if (ball.TurnPhase != TurnPhase.Shooting) return;
        }
        else {
            if (GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
                _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
            if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;
        }
        
        CurrentOccurrences++;
    }
}
