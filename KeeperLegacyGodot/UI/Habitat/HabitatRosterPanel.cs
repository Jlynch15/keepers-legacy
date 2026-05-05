// UI/Habitat/HabitatRosterPanel.cs
// 4-slot grid showing creatures in the active habitat. Empty slots have
// "Add Creature" buttons. Filled slots support tap (→ detail) and 600ms
// long-press (→ release confirm).

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;
using HabitatModel = KeeperLegacy.Models.Habitat;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatRosterPanel : PanelContainer
    {
        // ── Signals ───────────────────────────────────────────────────────────

        [Signal] public delegate void CreatureClickedEventHandler(string creatureId);
        [Signal] public delegate void AddCreatureRequestedEventHandler(int slotIndex);
        [Signal] public delegate void ReleaseCreatureRequestedEventHandler(string creatureId);

        // ── Layout constants ──────────────────────────────────────────────────

        private const float SlotPadding   = 10f;
        private const float HeaderPadding = 12f;
        private const float BlobSize      = 54f;
        internal const float LongPressMs  = 600f;

        // ── State ─────────────────────────────────────────────────────────────

        private HabitatModel? _habitat;
        private BiomeTheme? _theme;
        private int _habitatIndex; // 1-indexed for display

        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _capacityLabel;
        private GridContainer _grid;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetHabitat(HabitatModel habitat, BiomeTheme theme, int habitatIndex)
        {
            _habitat      = habitat;
            _theme        = theme;
            _habitatIndex = habitatIndex;
            Refresh();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor = HabitatPalette.RosterPanelBg;
            style.SetBorderWidthAll(0);
            AddThemeStyleboxOverride("panel", style);

            BuildShell();
        }

        private void BuildShell()
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 0);
            AddChild(vbox);

            // Header
            var header = new HBoxContainer();
            header.AddThemeConstantOverride("separation", 8);
            var headerMargin = new MarginContainer();
            headerMargin.AddThemeConstantOverride("margin_left",   (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_right",  (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_top",    (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_bottom", 8);
            headerMargin.AddChild(header);
            vbox.AddChild(headerMargin);

            var titleBox = new VBoxContainer();
            titleBox.AddThemeConstantOverride("separation", 1);
            titleBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(titleBox);

            _titleLabel = new Label();
            _titleLabel.AddThemeFontSizeOverride("font_size", 12);
            _titleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelName);
            titleBox.AddChild(_titleLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
            _subtitleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
            titleBox.AddChild(_subtitleLabel);

            _capacityLabel = new Label();
            _capacityLabel.AddThemeFontSizeOverride("font_size", 11);
            header.AddChild(_capacityLabel);

            // Grid
            _grid = new GridContainer();
            _grid.Columns = 2;
            _grid.AddThemeConstantOverride("h_separation", (int)SlotPadding);
            _grid.AddThemeConstantOverride("v_separation", (int)SlotPadding);
            var gridMargin = new MarginContainer();
            gridMargin.AddThemeConstantOverride("margin_left",   (int)SlotPadding);
            gridMargin.AddThemeConstantOverride("margin_right",  (int)SlotPadding);
            gridMargin.AddThemeConstantOverride("margin_bottom", (int)SlotPadding);
            gridMargin.AddChild(_grid);
            vbox.AddChild(gridMargin);
        }

        // ── Render ────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_habitat == null || _theme == null) return;

            _titleLabel.Text    = $"Habitat {_habitatIndex} — Roster";
            _subtitleLabel.Text = $"{_theme.IconEmoji} {_theme.DisplayName}";
            _capacityLabel.Text = $"{_habitat.OccupantIds.Count} / {HabitatCapacity.CreaturesPerHabitat}";
            _capacityLabel.AddThemeColorOverride("font_color", _theme.AccentColor);

            // Rebuild slot cells
            foreach (Node child in _grid.GetChildren()) child.QueueFree();
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
            {
                if (i < _habitat.OccupantIds.Count)
                    _grid.AddChild(BuildFilledSlot(_habitat.OccupantIds[i]));
                else
                    _grid.AddChild(BuildEmptySlot(i));
            }
        }

        private Control BuildFilledSlot(Guid creatureId)
        {
            var slot = new SlotControl(creatureId, _theme!.AccentColor);
            slot.CustomMinimumSize = new Vector2(140, 140);
            slot.Tapped       += () => EmitSignal(SignalName.CreatureClicked, creatureId.ToString());
            slot.LongPressed  += () => EmitSignal(SignalName.ReleaseCreatureRequested, creatureId.ToString());
            return slot;
        }

        private Control BuildEmptySlot(int slotIndex)
        {
            var slot = new EmptySlotControl(_theme!.AccentColor);
            slot.CustomMinimumSize = new Vector2(140, 140);
            slot.AddPressed += () => EmitSignal(SignalName.AddCreatureRequested, slotIndex);
            return slot;
        }

        // ── Inner classes ─────────────────────────────────────────────────────

        private partial class SlotControl : PanelContainer
        {
            [Signal] public delegate void TappedEventHandler();
            [Signal] public delegate void LongPressedEventHandler();

            private readonly Guid _creatureId;
            private readonly Color _accent;
            private float _holdMs;
            private bool _holding;
            private bool _longFired;

            public SlotControl(Guid creatureId, Color accent)
            {
                _creatureId = creatureId;
                _accent = accent;
            }

            public override void _Ready()
            {
                var style = new StyleBoxFlat();
                style.BgColor     = HabitatPalette.SlotBgIdle;
                style.BorderColor = _accent with { A = 0.30f };
                style.SetBorderWidthAll(2);
                style.SetCornerRadiusAll(10);
                AddThemeStyleboxOverride("panel", style);

                MouseFilter = MouseFilterEnum.Stop;

                var label = new Label();
                label.Text = "🐾";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment   = VerticalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 28);
                label.AddThemeColorOverride("font_color", HabitatPalette.LabelName);
                AddChild(label);
            }

            public override void _GuiInput(InputEvent @event)
            {
                if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _holding = true;
                        _longFired = false;
                        _holdMs = 0;
                    }
                    else if (_holding)
                    {
                        _holding = false;
                        if (!_longFired) EmitSignal(SignalName.Tapped);
                    }
                }
            }

            public override void _Process(double delta)
            {
                if (_holding && !_longFired)
                {
                    _holdMs += (float)delta * 1000f;
                    if (_holdMs >= HabitatRosterPanel.LongPressMs)
                    {
                        _longFired = true;
                        EmitSignal(SignalName.LongPressed);
                    }
                }
            }
        }

        private partial class EmptySlotControl : PanelContainer
        {
            [Signal] public delegate void AddPressedEventHandler();

            private readonly Color _accent;

            public EmptySlotControl(Color accent) { _accent = accent; }

            public override void _Ready()
            {
                var style = new StyleBoxFlat();
                style.BgColor = new Color(0, 0, 0, 0);
                style.BorderColor = _accent with { A = 0.20f };
                style.SetBorderWidthAll(2);
                style.SetCornerRadiusAll(10);
                // Note: Godot StyleBoxFlat doesn't support dashed borders natively;
                // the dashed look from the mockup is a polish detail we'll add via
                // a custom drawn ColorRect child later if Jesse wants it.
                AddThemeStyleboxOverride("panel", style);

                var vbox = new VBoxContainer();
                vbox.Alignment = BoxContainer.AlignmentMode.Center;
                vbox.AddThemeConstantOverride("separation", 4);
                AddChild(vbox);

                var plus = new Label();
                plus.Text = "+";
                plus.HorizontalAlignment = HorizontalAlignment.Center;
                plus.AddThemeFontSizeOverride("font_size", 26);
                plus.AddThemeColorOverride("font_color", _accent with { A = 0.40f });
                vbox.AddChild(plus);

                var label = new Label();
                label.Text = "Empty Slot";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 11);
                label.AddThemeColorOverride("font_color", _accent with { A = 0.55f });
                vbox.AddChild(label);

                var btn = new Button();
                btn.Text = "Add Creature";
                btn.AddThemeFontSizeOverride("font_size", 10);
                btn.FocusMode = FocusModeEnum.None;
                btn.Pressed += () => EmitSignal(SignalName.AddPressed);
                vbox.AddChild(btn);
            }
        }
    }
}
