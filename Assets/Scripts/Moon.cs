using System.Collections;
using UnityEngine;

public class Moon : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private bool _cooldown;
    private void OnTriggerEnter2D(Collider2D col) {
        if (!Utility.ActivateGoalTrigger(col.gameObject)) return;
        if (_cooldown) return;

        StartCoroutine(Cooldown());

        Utility.AddToNetworkTrick("moon");
        CurrentOccurrences++;
    }
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
