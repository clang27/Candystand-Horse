using System.Collections.Generic;
using System.Linq;

public class Player {
    public string Name { get; }
    public int Score { get; set; }
    public bool IsTurn { get; set; }
    public bool SetShot { get; set; }
    public bool Lost => Score == 5;

    public Player(string name) {
        Name = name;
        Score = 0;
        IsTurn = false;
        SetShot = false;
    }
    
    public static Player CurrentPlayer(List<Player> ap) {
        return ap.First(p => p.IsTurn);
    }

    public static Player NextPlayer(List<Player> ap) {
        var cp = CurrentPlayer(ap);
        
        return (ap.IndexOf(cp) == ap.Count - 1)
            ? ap[0]
            : ap[ap.IndexOf(cp) + 1];
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