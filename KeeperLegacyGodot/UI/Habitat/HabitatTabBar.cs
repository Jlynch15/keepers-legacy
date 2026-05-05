// UI/Habitat/HabitatTabBar.cs
// One tab per max habitat slot for the active biome. Owned tabs switch the
// active habitat; purchasable tabs trigger a buy dialog; story-gated tabs
// toast a hint.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;
using HabitatModel = KeeperLegacy.Models.Habitat;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatTabBar : PanelContainer
    {
        [Signal] public delegate void ActiveHabitatChangedEventHandler(string habitatId);
        [Signal] public delegate void BuyHabitatRequestedEventHandler(int slot);
        [Signal] public delegate void StoryGatedTappedEventHandler(int storyAct);

        private HabitatType _biome;
        private Guid? _activeHabitatId;
        private BiomeTheme? _theme;
        private HBoxContainer _tabBox;

        public void SetBiome(HabitatType biome, BiomeTheme theme)
        {
            _biome = biome;
            _theme = theme;
            Rebuild();
        }

        public void SetActiveHabitat(Guid habitatId)
        {
            _activeHabitatId = habitatId;
            Rebuild();
        }

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.031f, 0.024f, 0.016f, 0.90f);
            style.BorderWidthBottom = 1;
            style.BorderColor       = HabitatPalette.OverlayBarBorderTint;
            AddThemeStyleboxOverride("panel", style);

            _tabBox = new HBoxContainer();
            _tabBox.AddThemeConstantOverride("separation", 0);
            AddChild(_tabBox);
        }

        public void Rebuild()
        {
            if (_theme == null) return;
            foreach (Node child in _tabBox.GetChildren()) child.QueueFree();

            int max = HabitatCapacity.MaxHabitatsForBiome(_biome);
            var hm  = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;
            var owned = hm.HabitatsOfType(_biome);

            for (int slot = 1; slot <= max; slot++)
            {
                var reason = hm.GetUnlockReason(_biome, slot);
                _tabBox.AddChild(BuildTab(slot, reason, owned));
            }
        }

        private Control BuildTab(int slot, UnlockReason reason, IReadOnlyList<HabitatModel> owned)
        {
            var tab = new Button();
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.CustomMinimumSize   = new Vector2(0, 44); // 44px touch target
            tab.FocusMode           = FocusModeEnum.None;
            tab.AddThemeFontSizeOverride("font_size", 11);

            switch (reason.Kind)
            {
                case UnlockReasonKind.Owned:
                {
                    var habitat = owned[slot - 1];
                    bool active = habitat.Id == _activeHabitatId;
                    tab.Text = $"Habitat {slot}   {habitat.OccupantIds.Count}/{HabitatCapacity.CreaturesPerHabitat}";
                    tab.AddThemeColorOverride("font_color", active ? _theme!.AccentColor : HabitatPalette.LabelMuted);

                    var style = new StyleBoxFlat();
                    style.BgColor = active ? new Color(_theme!.AccentColor with { A = 0.08f }) : new Color(0,0,0,0);
                    style.BorderWidthBottom = 2;
                    style.BorderColor       = active ? _theme!.AccentColor : new Color(0,0,0,0);
                    tab.AddThemeStyleboxOverride("normal",   style);
                    tab.AddThemeStyleboxOverride("hover",    style);
                    tab.AddThemeStyleboxOverride("pressed",  style);

                    tab.Pressed += () => EmitSignal(SignalName.ActiveHabitatChanged, habitat.Id.ToString());
                    break;
                }

                case UnlockReasonKind.Purchasable:
                {
                    int cost = reason.Coins ?? 0;
                    tab.Text = $"🔒 Habitat {slot}\n✦ {cost}";
                    tab.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
                    tab.Pressed += () => EmitSignal(SignalName.BuyHabitatRequested, slot);
                    break;
                }

                case UnlockReasonKind.StoryGated:
                {
                    int act = reason.StoryAct ?? 1;
                    tab.Text = $"🔒 Habitat {slot}\nAct {ToRoman(act)}";
                    tab.AddThemeColorOverride("font_color", HabitatPalette.LabelLocked);
                    tab.Pressed += () => EmitSignal(SignalName.StoryGatedTapped, act);
                    break;
                }

                case UnlockReasonKind.OutOfRange:
                    tab.Visible = false;
                    break;
            }

            return tab;
        }

        private static string ToRoman(int n) => n switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => n.ToString()
        };
    }
}
