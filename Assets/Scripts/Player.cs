using System.Collections.Generic;
using System.Linq;

public class Player {
    public string Name { get; }
    public int Score { get; set; }
    public bool IsTurn { get; set; }
    public bool SetShot { get; set; }
    public bool IsAi { get; set; }
    public bool Lost => Score == MaxScore;
    private int MaxScore { get; }

    public Player(string name, int playerCount) {
        Name = name;
        Score = 0;
        IsTurn = false;
        SetShot = false;
        IsAi = false;
        MaxScore = playerCount switch {
            2 => 5,
            3 => 4,
            4 => 3,
            _ => 5
        };
    }
    
    public static Player CurrentPlayer(List<Player> ap) {
        return ap.First(p => p.IsTurn);
    }

    public static Player NextPlayer(List<Player> ap) {
        var cp = CurrentPlayer(ap);
        var index = ap.IndexOf(cp);
        var nextIndex = index == ap.Count - 1 ? 0 : index + 1;
        
        while(ap[nextIndex].Lost) {
            nextIndex = nextIndex == ap.Count - 1 ? 0 : nextIndex + 1;
        }

        return ap[nextIndex];
    }

    public static Player PlayerWhoSetAShot(List<Player> ap) {
        return ap.First(p => p.SetShot);
    }

    public static bool SomeHasAShotSet(List<Player> ap) {
        return ap.Any(p => p.SetShot);
    }

    public static void ClearAllShots(List<Player> ap) {
        ap.ForEach(p => p.SetShot = false);
    }

    public static void GoToNextPlayer(List<Player> ap) {
        var cp = CurrentPlayer(ap);
        var np = NextPlayer(ap);

        cp.IsTurn = false;
        np.IsTurn = true;
    }
}