using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Wobble;
using Wobble.Bindables;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Borders.Footer
{
    public class IconTextButtonMapPreview : IconTextButton
    {
        public IconTextButtonMapPreview(Bindable<LeftPanel> activeLeftPanel)
            : base(FontAwesome.Get(FontAwesomeIcon.fa_eye_open), FontManager.GetWobbleFont(Fonts.LatoBlack),
                "View Map", (sender, args) =>
                {
                    if (activeLeftPanel == null)
                        return;

                    var game = (QuaverGame)GameBase.Game;

                    if (activeLeftPanel.Value == LeftPanel.MapPreview)
                    {
                        if (game.CurrentScreen.Type == QuaverScreenType.Multiplayer)
                            activeLeftPanel.Value = LeftPanel.MatchSettings;
                        else
                            activeLeftPanel.Value = LeftPanel.Leaderboard;
                    }
                    else
                        activeLeftPanel.Value = LeftPanel.MapPreview;
                })
        {
        }
    }
}
