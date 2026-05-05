// UI/Habitat/ChoiceMenu.cs
// Lightweight floating choice panel anchored to a screen position. Tap-outside
// dismisses. Reusable; used here for "Add Creature" choices and could be reused
// elsewhere.

using Godot;
using System;
using System.Collections.Generic;

namespace KeeperLegacy.UI.Habitat
{
    public partial class ChoiceMenu : PanelContainer
    {
        public record ChoiceOption(string Label, bool Enabled, Action OnTap, string? DisabledReason = null);

        private VBoxContainer _box;
        private Action? _onDismiss;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            ZIndex = 100;

            var style = new StyleBoxFlat();
            style.BgColor               = HabitatPalette.ChoiceMenuBg;
            style.BorderColor           = HabitatPalette.ChoiceMenuBorder;
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(8);
            style.ContentMarginLeft     = 8;
            style.ContentMarginRight    = 8;
            style.ContentMarginTop      = 6;
            style.ContentMarginBottom   = 6;
            AddThemeStyleboxOverride("panel", style);

            _box = new VBoxContainer();
            _box.AddThemeConstantOverride("separation", 4);
            AddChild(_box);
        }

        /// <summary>
        /// Show the menu anchored at viewport position (top-left of the menu).
        /// onDismiss is called when the user taps outside or picks an option.
        /// </summary>
        public void Show(Vector2 anchor, IList<ChoiceOption> options, Action? onDismiss = null)
        {
            _onDismiss = onDismiss;
            foreach (Node child in _box.GetChildren()) child.QueueFree();

            foreach (var opt in options)
            {
                var btn = new Button();
                btn.Text = opt.Label + (!opt.Enabled && opt.DisabledReason != null ? $"  ({opt.DisabledReason})" : "");
                btn.Disabled = !opt.Enabled;
                btn.FocusMode = FocusModeEnum.None;
                btn.AddThemeFontSizeOverride("font_size", 12);
                if (opt.Enabled)
                {
                    btn.Pressed += () =>
                    {
                        opt.OnTap();
                        Dismiss();
                    };
                }
                _box.AddChild(btn);
            }

            Position = anchor;
            Visible  = true;
        }

        public void Dismiss()
        {
            Visible = false;
            _onDismiss?.Invoke();
        }

        public override void _GuiInput(InputEvent @event)
        {
            // Clicks on the menu itself are handled by the buttons — don't dismiss.
        }

        public override void _Input(InputEvent @event)
        {
            if (!Visible) return;
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                // If click is outside the menu rect, dismiss.
                Vector2 local = mb.Position - Position;
                if (local.X < 0 || local.Y < 0 || local.X > Size.X || local.Y > Size.Y)
                {
                    Dismiss();
                }
            }
        }
    }
}
