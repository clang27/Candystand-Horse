using System.Collections;
using UnityEngine;

public class EndGoal : MonoBehaviour {
    private BasketballGoal _goal;
    private bool _cooldown;
    private void Awake() {
        _goal = GetComponentInParent<BasketballGoal>();
    }
    private void OnTriggerEnter2D(Collider2D col) {
        if (!col.gameObject.name.Contains("Ball")) return;
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = col.gameObject.GetComponent<MPBasketball>();
            if (ball.TurnPhase is not TurnPhase.Shooting and not TurnPhase.Resting) return;
        }
        else {
            if (GameManager.Instance.TurnPhase is not TurnPhase.Shooting and not TurnPhase.Resting) return;
        }

        if (_cooldown) return;

        StartCoroutine(Cooldown());
        _goal.EndGoal(col.attachedRigidbody.velocity.sqrMagnitude);
    }
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
