using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StartGoal : MonoBehaviour{
    private BasketballGoal _goal;
    private bool _cooldown;
    private Boombox _boombox;
    private List<AudioSource> _audioSources;
    private void Awake() {
        _goal = GetComponentInParent<BasketballGoal>();
        _audioSources = FindObjectsOfType<AudioSource>().ToList();
    }
    private void OnTriggerEnter2D(Collider2D col) {
        if (_cooldown) return;
        if (!Utility.ActivateGoalTrigger(col.gameObject)) return;
        
        StartCoroutine(CooldownPlusSlomo());
        _goal.StartGoal();
    }

    private void OnTriggerStay2D(Collider2D col) {
        if (_cooldown) return;
        if (!Utility.ActivateGoalTrigger(col.gameObject)) return;
        
        StartCoroutine(CooldownPlusSlomo());
    }

    private IEnumerator CooldownPlusSlomo() {
        if (GameManager.Instance.PotentialLastShot()) {
            if (MPSpawner.Boombox)
                MPSpawner.Boombox.AudioSource.pitch = 0.4f;

            foreach (var audio in _audioSources)
                audio.pitch = 0.4f;
            Time.timeScale = 0.4f;
        }

        _cooldown = true;
        yield return new WaitForSeconds(0.2f);
        _cooldown = false;

        if (GameManager.Instance.PotentialLastShot()) {
            if (MPSpawner.Boombox)
                MPSpawner.Boombox.AudioSource.pitch = 1f;
            foreach (var audio in _audioSources)
                audio.pitch = 1f;

            Time.timeScale = 1f;
        }
    }
}
