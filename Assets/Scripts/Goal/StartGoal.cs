using UnityEngine;

public class StartGoal : MonoBehaviour{
    private BasketballGoal _goal;
    private void Awake() {
        _goal = GetComponentInParent<BasketballGoal>();
    }
    private void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (GameManager.Instance.TurnPhase is not TurnPhase.Shooting and not TurnPhase.Resting) return;

        _goal.StartGoal();
    }
}
