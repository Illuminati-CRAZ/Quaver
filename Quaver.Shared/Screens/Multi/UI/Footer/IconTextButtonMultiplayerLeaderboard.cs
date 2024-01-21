using Quaver.Server.Common.Objects.Multiplayer;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Selection.UI;
using Wobble.Bindables;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Multi.UI.Footer
{
    public class IconTextButtonMultiplayerLeaderboard : IconTextButton
    {
        public IconTextButtonMultiplayerLeaderboard(MultiplayerGameScreen screen)
            : base(FontAwesome.Get(FontAwesomeIcon.fa_trophy),
            FontManager.GetWobbleFont(Fonts.LatoBlack),"Leaderboard", (sender, args) =>
            {
                if (screen.ActiveLeftPanel.Value == LeftPanel.Leaderboard)
                    screen.ActiveLeftPanel.Value = LeftPanel.MatchSettings;
                else
                    screen.ActiveLeftPanel.Value = LeftPanel.Leaderboard;
            })
        {
        }
    }
}
