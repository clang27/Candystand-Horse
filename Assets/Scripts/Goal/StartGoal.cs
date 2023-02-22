using System.Collections;
using UnityEngine;

public class StartGoal : MonoBehaviour{
    private BasketballGoal _goal;
    private bool _cooldown;
    private void Awake() {
        _goal = GetComponentInParent<BasketballGoal>();
    }
    private void OnTriggerEnter2D(Collider2D col) {
        if (!Utility.ActivateGoalTrigger(col.gameObject)) return;
        if (_cooldown) return;

        StartCoroutine(Cooldown());
        _goal.StartGoal();
    }
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
