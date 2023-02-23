using System.Collections;
using UnityEngine;

public class Wall : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private BasketballSounds _sounds;
    private bool _cooldown;

    private void Awake() {
        _sounds = GetComponent<BasketballSounds>();
    }

    private void OnCollisionEnter2D(Collision2D col) {
        if (_cooldown) return;
        if (Utility.PlayBallSound(col.gameObject))
            _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
        
        if (!Utility.ActivateShotCollision(col.gameObject)) return;

        StartCoroutine(Cooldown());
        CurrentOccurrences++;
    }
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
