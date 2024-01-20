using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Screens.Selection.UI;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Screens;
using IDrawable = Wobble.Graphics.IDrawable;

namespace Quaver.Shared.Screens
{
	public abstract class LeftPanelScreenView : ScreenView
	{
		public new IHasLeftPanel Screen { get; }

		public Dictionary<LeftPanels, Sprite> Panels { get; } = new Dictionary<LeftPanels, Sprite>();

		private const int ScreenPaddingX = 50;

		public LeftPanelScreenView(Screen screen) : base(screen)
		{
			// CreatePanels();

			Screen = (IHasLeftPanel)screen;
			Screen.ActiveLeftPanel.ValueChanged += OnActiveLeftPanelChanged;
		}

		// populate Panels with Key, Value pairs of the panels wanted
		// value = an instance of the panel
		// key = the LeftPanels enum value associated with the panel
		// public abstract void CreatePanels();

		private void OnActiveLeftPanelChanged(object sender, BindableValueChangedEventArgs<LeftPanels> e)
		{
			const int animTime = 400;
			const Easing easing = Easing.OutQuint;
			// var inactivePos = -Leaderboard.Width - ScreenPaddingX;
			const int inactivePos = -564 - ScreenPaddingX;

			foreach (var pair in Panels)
			{
				pair.Value.ClearAnimations();

				if (e.Value == pair.Key)
				{
					pair.Value.MoveToX(ScreenPaddingX, easing, animTime);
				}
				else
				{
					pair.Value.MoveToX(inactivePos, easing, animTime);
				}
			}
		}
	}
}
