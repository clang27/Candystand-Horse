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
}
