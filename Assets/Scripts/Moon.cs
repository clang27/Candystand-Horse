using UnityEngine;

public class Moon : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private void OnTriggerEnter2D(Collider2D col) {
        if (!col.gameObject.name.Contains("Ball")) return;
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = col.gameObject.GetComponent<MPBasketball>();
            if (ball.TurnPhase is not TurnPhase.Shooting and not TurnPhase.Resting) return;
        }
        else {
            if (GameManager.Instance.TurnPhase is not TurnPhase.Shooting and not TurnPhase.Resting) return;
        }

        CurrentOccurrences++;
    }
}
