using System.Collections.Generic;
using UnityEngine;

public class BasketballSounds : MonoBehaviour {
    [SerializeField] private List<AudioClip> loudSounds, quietSounds;
    [SerializeField] private float loudThreshold = 400f;
    
    private AudioSource _audioSource;
    
    private void Awake() {
        _audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(float collisionStrength) {
        if (Time.timeScale > 0.9f)
            _audioSource.pitch = Random.Range(0.96f, 1.04f);
        
        _audioSource.volume = Mathf.Clamp(Mathf.Sqrt(collisionStrength) / 25f, 0.01f, 1f);
        
        if (collisionStrength > loudThreshold && loudSounds.Count > 0) {
            _audioSource.PlayOneShot(loudSounds[Random.Range(0, loudSounds.Count)]);
        } else if (quietSounds.Count > 0) {
            _audioSource.PlayOneShot(quietSounds[Random.Range(0, quietSounds.Count)]);
        }
    }
}
