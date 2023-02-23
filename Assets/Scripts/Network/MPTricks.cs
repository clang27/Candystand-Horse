using Fusion;

public class MPTricks : NetworkBehaviour {
    [Networked(OnChanged = nameof(OnTrickUpdate))] private ushort ServerTricks { get; set; }
    [Networked(OnChanged = nameof(OnClearUpdate))] private NetworkBool ClearFlags { get; set; }
    
    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetworkInputData data)) return;
        if (!Runner.IsServer || data.Tricks == 0) return;

        if (ServerTricks != data.Tricks)
            ServerTricks = data.Tricks;
        else
            ClearFlags = !ClearFlags;
    }
    private static void OnTrickUpdate(Changed<MPTricks> changed) {
        var t = changed.Behaviour.ServerTricks;
        
        if ((t & NetworkInputData.TRICK1) != 0)
            TrickShot.SelectTrickShotWithInput(1);
        else if ((t & NetworkInputData.TRICK2) != 0)
            TrickShot.SelectTrickShotWithInput(2);
        else if ((t & NetworkInputData.TRICK3) != 0)
            TrickShot.SelectTrickShotWithInput(3);
        else if ((t & NetworkInputData.TRICK4) != 0)
            TrickShot.SelectTrickShotWithInput(4);
        else if ((t & NetworkInputData.TRICK5) != 0)
            TrickShot.SelectTrickShotWithInput(5);
        else if ((t & NetworkInputData.TRICK6) != 0)
            TrickShot.SelectTrickShotWithInput(6);
        else if ((t & NetworkInputData.TRICK7) != 0)
            TrickShot.SelectTrickShotWithInput(7);
        else if ((t & NetworkInputData.TRICK8) != 0)
            TrickShot.SelectTrickShotWithInput(8);
        else if ((t & NetworkInputData.TRICK9) != 0)
            TrickShot.SelectTrickShotWithInput(9);
    }

    private static void OnClearUpdate(Changed<MPTricks> changed) {
        var t = changed.Behaviour.ServerTricks;
        
        if ((t & NetworkInputData.TRICK1) != 0)
            TrickShot.SelectTrickShotWithInput(1);
        else if ((t & NetworkInputData.TRICK2) != 0)
            TrickShot.SelectTrickShotWithInput(2);
        else if ((t & NetworkInputData.TRICK3) != 0)
            TrickShot.SelectTrickShotWithInput(3);
        else if ((t & NetworkInputData.TRICK4) != 0)
            TrickShot.SelectTrickShotWithInput(4);
        else if ((t & NetworkInputData.TRICK5) != 0)
            TrickShot.SelectTrickShotWithInput(5);
        else if ((t & NetworkInputData.TRICK6) != 0)
            TrickShot.SelectTrickShotWithInput(6);
        else if ((t & NetworkInputData.TRICK7) != 0)
            TrickShot.SelectTrickShotWithInput(7);
        else if ((t & NetworkInputData.TRICK8) != 0)
            TrickShot.SelectTrickShotWithInput(8);
        else if ((t & NetworkInputData.TRICK9) != 0)
            TrickShot.SelectTrickShotWithInput(9);
    }
}