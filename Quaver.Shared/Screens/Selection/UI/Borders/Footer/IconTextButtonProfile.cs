using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Menu.Border.Components;
using Wobble.Bindables;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Selection.UI.Borders.Footer
{
    public class IconTextButtonProfile : IconTextButton
    {
        public IconTextButtonProfile(Bindable<LeftPanels> activeLeftPanel)
            : base(FontAwesome.Get(FontAwesomeIcon.fa_user_shape), FontManager.GetWobbleFont(Fonts.LatoBlack),
                "Profile", (sender, args) =>
                {
                    if (activeLeftPanel == null)
                        return;

                    if (activeLeftPanel.Value == LeftPanels.UserProfile)
                        activeLeftPanel.Value = LeftPanels.Leaderboard;
                    else
                        activeLeftPanel.Value = LeftPanels.UserProfile;
                })
        {
        }
    }
}
