using TMPro;
using UnityEngine;

public class TimerUi : MonoBehaviour {
    public bool Countdown { get; private set; }
    private TextMeshProUGUI _leftDigit, _rightDigit;
    private float _timer;
    private AudioSource _audioSource;
    
    private void Awake() {
        _leftDigit = GetComponentsInChildren<TextMeshProUGUI>()[0];
        _rightDigit = GetComponentsInChildren<TextMeshProUGUI>()[1];
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update() {
        if (!Countdown) return;

        _timer -= Time.deltaTime;
        
        if (_timer <= 0f) {
            Countdown = false;
            _timer = 0f;
            _audioSource.Play();
            GameManager.Instance.NextPlayersTurn();
        }
        
        UpdateTimer(_timer);
    }

    public void StartCountdown(float t) {
        Countdown = true;
        _timer = t;
        UpdateTimer(_timer);
    }

    public void EndCountdown() {
        Countdown = false;
    }

    private void UpdateTimer(float time) {
        var ld = Mathf.CeilToInt(time).ToString("00")[0].ToString();
        var rd = Mathf.CeilToInt(time).ToString("00")[1].ToString();

        _leftDigit.text = ld;
        _rightDigit.text = rd;
    }
}
