using System.Collections;
using UnityEngine;

public class EndGoal : MonoBehaviour {
    private BasketballGoal _goal;
    private bool _cooldown;
    private void Awake() {
        _goal = GetComponentInParent<BasketballGoal>();
    }
    private void OnTriggerEnter2D(Collider2D col) {
        if (_cooldown) return;
        if (!Utility.ActivateGoalTrigger(col.gameObject)) return;

        StartCoroutine(Cooldown());
        _goal.EndGoal(col.attachedRigidbody.velocity.sqrMagnitude);
    }
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
