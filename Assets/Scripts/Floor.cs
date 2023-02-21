using System.Collections;
using UnityEngine;

public class Floor : MonoBehaviour, IShot {
    [SerializeField] private float minimumSpeed = 25f;
    public int CurrentOccurrences { get; set; }
    private BasketballSounds _sounds;
    private bool _cooldown;

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

        if (_cooldown) return;
        
        if (col.relativeVelocity.y < 0f && col.relativeVelocity.y > -minimumSpeed) {
            GameManager.Instance.ShotMissed(col.gameObject);
            StartCoroutine(Cooldown());
        }
    }
    
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
