using System.Linq;
using UnityEngine;

public static class Utility {
    public static bool ObjectIsBall(GameObject go) {
        return go.name.Contains("Ball");
    }
    public static bool ActivateGoalTrigger(GameObject go) {
        if (!ObjectIsBall(go)) 
            return false;
        
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = go.GetComponent<MPBasketball>();
            return ball && ball.TurnPhase is TurnPhase.Shooting or TurnPhase.Resting;
        }

        return GameManager.Instance.TurnPhase is TurnPhase.Shooting or TurnPhase.Resting;
    }

    public static bool ActivateShotCollision(GameObject go) {
        if (!ObjectIsBall(go))
            return false;
        
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = go.GetComponent<MPBasketball>();
            return ball && ball.TurnPhase is TurnPhase.Shooting;
        }

        return GameManager.Instance.TurnPhase is TurnPhase.Shooting;
    }

    public static bool PlayBallSound(GameObject go) {
        if (!ObjectIsBall(go))
            return false;
        
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = go.GetComponent<MPBasketball>();
            return ball && ball.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging;
        }

        return GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging;
    }

    // TODO: Remove if doing input
    public static void AddToNetworkTrick(string subname) {
        if (!MPSpawner.Ball || !MPSpawner.Ball.Player.IsTurn || !MPSpawner.Ball.IsMe) return;

        var tricksToUpdate = MPSpawner.Ball.ClientTricks
            .Where(t => t.Name.ToLower().Contains(subname.ToLower()))
            .ToList();
        
        foreach (var t1 in tricksToUpdate
                     .Where(t1 => MPSpawner.Ball.ClientTricks.Any(t2 => t2.Name.Equals(t1.Name)))) {
            for (var i = 0; i < MPSpawner.Ball.ClientTricks.Length; i++) {
                if (!MPSpawner.Ball.ClientTricks[i].Name.Equals(t1.Name)) continue;
                
                MPSpawner.Ball.ClientTricks[i].Occurrences++;
            }
        }
    }
}
