using Quaver.API.Enums;
using Quaver.Server.Client;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Helpers;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Menu.Border.Components.Buttons
{
    public class IconTextButtonLeaderboards: IconTextButton
    {
        public IconTextButtonLeaderboards() : base(FontAwesome.Get(FontAwesomeIcon.fa_trophy),
            FontManager.GetWobbleFont(Fonts.LatoBlack),"Leaderboards", (sender, args) =>
            {
                var mode = ConfigManager.SelectedGameMode?.Value ?? GameMode.Keys4;
                BrowserHelper.OpenURL($"{OnlineClient.WEBSITE_URL}/leaderboard/?mode={(int) mode}");
            })
        {
        }
    }
}
