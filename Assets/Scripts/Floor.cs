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
        if (_cooldown) return;
        if (Utility.PlayBallSound(col.gameObject))
            _sounds.PlaySound(col.relativeVelocity.sqrMagnitude);
        
        if (!Utility.ActivateShotCollision(col.gameObject)) return;

        StartCoroutine(Cooldown());
        CurrentOccurrences++;

        if (col.relativeVelocity.y < 0f && col.relativeVelocity.y > -minimumSpeed) {
            GameManager.Instance.ShotMissed(col.gameObject);
            StartCoroutine(Cooldown());
        }
    }
    
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.25f);
        _cooldown = false;
    }
}
