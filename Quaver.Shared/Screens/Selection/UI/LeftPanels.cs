using System;

// TODO: change namespace?
namespace Quaver.Shared.Screens.Selection.UI
{
    [Flags]
    public enum LeftPanels
    {
        Leaderboard = 1 << 0,
        Modifiers = 1 << 1,
        MatchSettings = 1 << 2,
        MapPreview = 1 << 3,
        UserProfile = 1 << 4,
        MultiplayerMatchSettings = 1 << 5
    }
}
