using UnityEngine;

public class Moon : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;

        CurrentOccurrences++;
    }
}
