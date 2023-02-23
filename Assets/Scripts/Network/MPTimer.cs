using Fusion;
using TMPro;
using UnityEngine;

public class MPTimer : NetworkBehaviour { 
    [Networked] public TickTimer Timer { get; set; }
    [Networked(OnChanged = nameof(OnTimerUpdated))] public NetworkBool OutOfTime { get; set; }
    [Networked] public int Seconds { get; set; } = 25;

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
        if (!Timer.Expired(Runner)) return;
        
        Timer = TickTimer.None;
        OutOfTime = true;
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
    private static void OnTimerUpdated(Changed<MPTimer> changed) {
        if (!changed.Behaviour.OutOfTime) return;
        
        MPSpawner.Timer._audioSource.Play();
        GameManager.Instance.NextMPPlayersTurn();

        changed.Behaviour.OutOfTime = false;
    }
}