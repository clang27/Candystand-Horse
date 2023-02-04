using UnityEngine;

public class Floor : MonoBehaviour, IShot {
    [SerializeField] private float minimumSpeed = 25f;
    public int CurrentOccurrences { get; set; }
    private bool _ballTouchingGround;
    private BasketballSounds _sounds;

    private void Awake() {
        _sounds = GetComponent<BasketballSounds>();
    }
    private void OnCollisionEnter2D(Collision2D col) {
        if (col.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
            _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
        if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;

        _ballTouchingGround = true;
        CurrentOccurrences++;

        if (!GameManager.Instance.InLobby && col.relativeVelocity.y < 0f && col.relativeVelocity.y > -minimumSpeed) {
            GameManager.Instance.NextPlayersTurn();
        }
    }

    private void OnCollisionExit2D(Collision2D col) {
        if (col.gameObject.layer == LayerMask.NameToLayer("Ball")) return;
        if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;
        if (!_ballTouchingGround) return;
        
        _ballTouchingGround = false;
    }
}
