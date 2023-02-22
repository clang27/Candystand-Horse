using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class MPPlayer : NetworkBehaviour {
    public MPBasketball Basketball { get; private set; }
    [Networked] public string Name { get; set; }
    [Networked(OnChanged = nameof(OnScoreChanged))] public int Score { get; set; }
    [Networked(OnChanged = nameof(OnTurnChanged))] public NetworkBool IsTurn { get; private set; }
    [Networked] public NetworkBool SetShot { get; set; }
    [Networked(OnChanged = nameof(OnIndexChanged))] public int PlayerIndex { get; private set; }
    [Networked(OnChanged = nameof(OnCountChanged))] private int PlayerCount { get; set; }

    public bool Lost => Score == MaxScore;
    private int MaxScore => PlayerCount switch {
        2 => 5,
        3 => 4,
        4 => 3,
        _ => 5
    };

    private void Awake() {
        Basketball = GetComponent<MPBasketball>();
    }

    public override void Spawned() {
        PlayerCount = MPBasketball.Players.Count;
        PlayerIndex = PlayerCount - 1;
        IsTurn = PlayerIndex == 0;
        Name = "P" + PlayerCount;
        gameObject.name = "Ball_" + Name;
        
        var c = PlayerIndex switch {
            0 => Color.red,
            1 => Color.green,
            2 => Color.yellow,
            3 => Color.cyan,
            _ => Color.HSVToRGB(0.07f, 1f, 1f)
        };
        
        Basketball.ChangeColor(c);
    }

    public static MPPlayer CurrentPlayer(List<MPPlayer> ap) {
        return ap.First(p => p.IsTurn);
    }

    public static MPPlayer NextPlayer(List<MPPlayer> ap) {
        var cp = CurrentPlayer(ap);
        var nextIndex = cp.PlayerIndex == ap.Count - 1 ? 0 : cp.PlayerIndex + 1;
        
        while(ap[nextIndex].Lost) {
            nextIndex = nextIndex == ap.Count - 1 ? 0 : nextIndex + 1;
        }

        return ap[nextIndex];
    }

    public static MPPlayer PlayerWhoSetAShot(List<MPPlayer> ap) {
        return ap.First(p => p.SetShot);
    }

    public static bool SomeHasAShotSet(List<MPPlayer> ap) {
        return ap.Any(p => p.SetShot);
    }

    public static void ClearAllShots(List<MPPlayer> ap) {
        ap.ForEach(p => p.SetShot = false);
    }

    public static void GoToNextPlayer(List<MPPlayer> ap) {
        var cp = CurrentPlayer(ap);
        var np = NextPlayer(ap);

        cp.IsTurn = false;
        np.IsTurn = true;
    }
    
    public static void OnScoreChanged(Changed<MPPlayer> changed) {
        GameUiManager.Instance.UpdateMPScore(MPBasketball.Players);
    }
    
    public static void OnTurnChanged(Changed<MPPlayer> changed) {
        GameUiManager.Instance.UpdateMPScore(MPBasketball.Players);

        var bb = changed.Behaviour.Basketball;
        if (bb.IsMe && bb.Player.IsTurn && bb.TurnPhase is not TurnPhase.Responding)
            TrickShotsSelector.Instance.ActivateButton(true);
    }
    
    public static void OnIndexChanged(Changed<MPPlayer> changed) {
        changed.Behaviour.Name = "P" + changed.Behaviour.PlayerCount;
        changed.Behaviour.gameObject.name = "Ball_" + changed.Behaviour.Name;
        
        GameUiManager.Instance.UpdateMPScore(MPBasketball.Players);
        
        var c = changed.Behaviour.PlayerIndex switch {
            0 => Color.red,
            1 => Color.green,
            2 => Color.yellow,
            3 => Color.cyan,
            _ => Color.HSVToRGB(0.07f, 1f, 1f)
        };
        
        changed.Behaviour.Basketball.ChangeColor(c);
    }
    
    public static void OnCountChanged(Changed<MPPlayer> changed) {
        GameUiManager.Instance.UpdateMPScore(MPBasketball.Players);
    }
}
