using Fusion;
using TMPro;
using UnityEngine;

public class MPTimer : NetworkBehaviour { 
    [Networked] public TickTimer Timer { get; set; }
    [Networked] public int Seconds { get; set; } = 15;

    private TextMeshProUGUI _leftDigit, _rightDigit;
    private AudioSource _audioSource;
    
    private void Awake() {
        _leftDigit = GetComponentsInChildren<TextMeshProUGUI>()[0];
        _rightDigit = GetComponentsInChildren<TextMeshProUGUI>()[1];
        _audioSource = GetComponent<AudioSource>();
    }
    
    public override void Spawned() {
        if (!MPSpawner.Timer)
            MPSpawner.Timer = this;
    }

    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetworkInputData data)) return;

        if (!Timer.Expired(Runner)) return;
        
        Timer = TickTimer.None;
        _audioSource.Play();
        GameManager.Instance.NextMPPlayersTurn();
    }

    public override void Render() {
        UpdateTimer(Timer.RemainingTime(Runner).HasValue ? 
            Timer.RemainingTime(Runner).Value :
            0f);
    }

    private void UpdateTimer(float time) {
        var ld = Mathf.CeilToInt(time).ToString("00")[0].ToString();
        var rd = Mathf.CeilToInt(time).ToString("00")[1].ToString();

        _leftDigit.text = ld;
        _rightDigit.text = rd;
    }
}