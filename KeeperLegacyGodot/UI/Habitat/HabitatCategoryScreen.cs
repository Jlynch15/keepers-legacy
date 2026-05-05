// UI/Habitat/HabitatCategoryScreen.cs
// Orchestrator for the Habitat Category screen — wires the four UI children
// to manager calls and to each other.

using Godot;
using System;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;
using HabitatModel = KeeperLegacy.Models.Habitat;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatCategoryScreen : Control
    {
        // Static — set by Detail screen so we know who's selected post-navigation.
        public static Guid? SelectedCreatureId { get; set; }

        private HabitatType _biome;
        private HabitatModel? _activeHabitat;

        private HabitatOverlayBar      _overlayBar;
        private HabitatTabBar          _tabBar;
        private HabitatEnvironmentView _envView;
        private HabitatRosterPanel     _rosterPanel;
        private ChoiceMenu             _choiceMenu;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void _Ready()
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _biome = HabitatFloorScreen.SelectedHabitatType ?? HabitatType.Water;

            BuildLayout();
            WireSignals();
            LoadInitialState();
        }

        public override void _ExitTree()
        {
            UnwireSignals();
        }

        public override void _EnterTree()
        {
            // Re-pull data when returning from a sub-screen — defensive in case
            // creatures were deleted while on Detail.
            if (_activeHabitat != null) RefreshChildren();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            var theme = BiomeThemes.For(_biome);
            if (theme == null)
            {
                GD.PushWarning($"No BiomeTheme registered for {_biome} — using neutral fallback");
                return;
            }

            // Root vbox: overlay bar + tab bar + content area
            var root = new VBoxContainer();
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddThemeConstantOverride("separation", 0);
            AddChild(root);

            _overlayBar = new HabitatOverlayBar();
            _overlayBar.SetTheme(theme);
            _overlayBar.BackPressed += OnBackPressed;
            root.AddChild(_overlayBar);

            _tabBar = new HabitatTabBar();
            _tabBar.SetBiome(_biome, theme);
            _tabBar.ActiveHabitatChanged += OnTabActiveHabitatChanged;
            _tabBar.BuyHabitatRequested  += OnBuyHabitatRequested;
            _tabBar.StoryGatedTapped     += OnStoryGatedTapped;
            root.AddChild(_tabBar);

            // Content area — env view + roster panel
            var content = new HBoxContainer();
            content.SizeFlagsVertical   = SizeFlags.ExpandFill;
            content.AddThemeConstantOverride("separation", 0);
            root.AddChild(content);

            _envView = new HabitatEnvironmentView();
            _envView.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _envView.SetTheme(theme);
            _envView.CreatureClicked += OnCreatureClicked;
            content.AddChild(_envView);

            _rosterPanel = new HabitatRosterPanel();
            _rosterPanel.CustomMinimumSize = new Vector2(360, 0);
            _rosterPanel.CreatureClicked          += OnCreatureClicked;
            _rosterPanel.AddCreatureRequested     += OnAddCreatureRequested;
            _rosterPanel.ReleaseCreatureRequested += OnReleaseCreatureRequested;
            content.AddChild(_rosterPanel);

            // Choice menu — added at root for top z-index
            _choiceMenu = new ChoiceMenu();
            _choiceMenu.Visible = false;
            AddChild(_choiceMenu);
        }

        // ── Initial state ─────────────────────────────────────────────────────

        private void LoadInitialState()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;

            var owned = hm.HabitatsOfType(_biome);
            _activeHabitat = owned.FirstOrDefault();
            if (_activeHabitat != null) _tabBar.SetActiveHabitat(_activeHabitat.Id);

            RefreshChildren();
            RefreshOverlayBarCoins();
            RefreshOverlayBarCapacity();
        }

        // ── Manager signal subscriptions ──────────────────────────────────────

        private void WireSignals()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

            if (hm != null)
            {
                hm.HabitatsChanged  += OnHabitatsChanged;
                hm.CreaturesChanged += OnCreaturesChanged;
            }
            if (pm != null)
            {
                pm.CoinsChanged    += OnCoinsChanged;
                pm.FeatureUnlocked += OnFeatureUnlocked;
            }
        }

        private void UnwireSignals()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

            if (hm != null)
            {
                hm.HabitatsChanged  -= OnHabitatsChanged;
                hm.CreaturesChanged -= OnCreaturesChanged;
            }
            if (pm != null)
            {
                pm.CoinsChanged    -= OnCoinsChanged;
                pm.FeatureUnlocked -= OnFeatureUnlocked;
            }
        }

        private void OnHabitatsChanged()
        {
            _tabBar.Rebuild();
            // Defensive — if active was deleted, reset to first owned
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;
            if (_activeHabitat == null || !hm.Habitats.Contains(_activeHabitat))
            {
                _activeHabitat = hm.HabitatsOfType(_biome).FirstOrDefault();
            }
            if (_activeHabitat != null) _tabBar.SetActiveHabitat(_activeHabitat.Id);
            RefreshChildren();
            RefreshOverlayBarCapacity();
        }

        private void OnCreaturesChanged()
        {
            RefreshChildren();
            RefreshOverlayBarCapacity();
        }

        private void OnCoinsChanged(int coins)
        {
            _tabBar.Rebuild();
            RefreshOverlayBarCoins();
        }

        private void OnFeatureUnlocked(string featureRaw)
        {
            _tabBar.Rebuild();
        }

        // ── User action handlers ──────────────────────────────────────────────

        private void OnBackPressed()
        {
            FindMainScene()?.NavigateBack();
        }

        private void OnTabActiveHabitatChanged(string habitatIdStr)
        {
            if (!Guid.TryParse(habitatIdStr, out var id)) return;
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            _activeHabitat = hm?.GetHabitat(id);
            if (_activeHabitat == null) return;
            _tabBar.SetActiveHabitat(id);
            RefreshChildren();
        }

        private void OnBuyHabitatRequested(int slot)
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            if (hm == null || pm == null) return;

            int cost = HabitatCapacity.CoinsForHabitat(_biome, slot);
            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: $"Buy Habitat {slot}  (✦ {cost})",
                        Enabled: pm.Coins >= cost,
                        DisabledReason: pm.Coins < cost ? $"Have ✦ {pm.Coins}" : null,
                        OnTap: () =>
                        {
                            if (hm.TryAddHabitat(_biome, out _))
                            {
                                var owned = hm.HabitatsOfType(_biome);
                                if (owned.Count > 0)
                                {
                                    _activeHabitat = owned[owned.Count - 1];
                                    _tabBar.SetActiveHabitat(_activeHabitat.Id);
                                    RefreshChildren();
                                }
                            }
                        }),
                    new ChoiceMenu.ChoiceOption(Label: "Cancel", Enabled: true, OnTap: () => { })
                });
        }

        private void OnStoryGatedTapped(int storyAct)
        {
            // Toast — a dedicated Toast component is out of scope; print a console
            // message and a console-visible debug line so QA can see it.
            GD.Print($"[Toast] Continue your story to unlock — Act {storyAct}");
        }

        private void OnCreatureClicked(string creatureIdStr)
        {
            if (!Guid.TryParse(creatureIdStr, out var id)) return;
            SelectedCreatureId = id;
            FindMainScene()?.NavigateToSubScreen("HabitatDetail");
        }

        private void OnAddCreatureRequested(int slotIndex)
        {
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            bool breedingUnlocked = pm?.IsFeatureUnlocked(GameFeature.Breeding) ?? false;

            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: "Buy from Shop",
                        Enabled: true,
                        OnTap: () => FindMainScene()?.NavigateTo("Shop")),
                    new ChoiceMenu.ChoiceOption(
                        Label: "Breed New",
                        Enabled: breedingUnlocked,
                        DisabledReason: breedingUnlocked ? null : "Locked",
                        OnTap: () => FindMainScene()?.NavigateTo("Breed")),
                });
        }

        private void OnReleaseCreatureRequested(string creatureIdStr)
        {
            if (!Guid.TryParse(creatureIdStr, out var id)) return;
            if (_activeHabitat == null) return;

            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            string name = hm?.GetCreature(id) is { } c
                ? CreatureRosterData.Find(c.CatalogId)?.Name ?? "this creature"
                : "this creature";

            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: $"Release {name}",
                        Enabled: true,
                        OnTap: () =>
                        {
                            bool ok = hm?.ReleaseCreature(_activeHabitat.Id, id) ?? false;
                            if (!ok) GD.Print($"[Toast] Cannot release — {name} is reserved by an active customer order.");
                        }),
                    new ChoiceMenu.ChoiceOption(Label: "Cancel", Enabled: true, OnTap: () => { })
                });
        }

        // ── Refresh helpers ───────────────────────────────────────────────────

        private void RefreshChildren()
        {
            if (_activeHabitat == null) return;
            var theme = BiomeThemes.For(_biome);
            if (theme == null) return;

            var hm    = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            int index = (hm?.HabitatsOfType(_biome).ToList().IndexOf(_activeHabitat) ?? 0) + 1;

            _envView.SetHabitat(_activeHabitat);
            _rosterPanel.SetHabitat(_activeHabitat, theme, index);
        }

        private void RefreshOverlayBarCoins()
        {
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            _overlayBar.SetCoinsText(pm?.Coins ?? 0);
        }

        private void RefreshOverlayBarCapacity()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            int total = 0;
            if (hm != null)
            {
                foreach (var h in hm.HabitatsOfType(_biome))
                    total += h.OccupantIds.Count;
            }
            int max = HabitatCapacity.MaxHabitatsForBiome(_biome) * HabitatCapacity.CreaturesPerHabitat;
            _overlayBar.SetCapacityText(total, max);
        }

        // ── Main scene lookup ─────────────────────────────────────────────────

        private MainScene? FindMainScene()
        {
            Node? n = this;
            while (n != null)
            {
                if (n is MainScene ms) return ms;
                n = n.GetParent();
            }
            return null;
        }

        // ── Debug surface ─────────────────────────────────────────────────────

        private static readonly float[] DecorationScales = { 0.5f, 0.7f, 1.0f, 1.5f, 2.0f, 3.0f };
        private int _decoScaleIndex = 2; // 1.0x

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

            switch (key.Keycode)
            {
                case Key.F4:
                case Key.P:
                    HabitatEnvironmentView.DecorationNode.DebugDragEnabled = !HabitatEnvironmentView.DecorationNode.DebugDragEnabled;
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled)
                    {
                        PrintBakeValues();
                    }
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.O:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    var focused = HabitatEnvironmentView.DecorationNode.FocusedNode;
                    if (focused == null) return;
                    _decoScaleIndex = (_decoScaleIndex + 1) % DecorationScales.Length;
                    focused.ScaleMultiplier = DecorationScales[_decoScaleIndex];
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.Tab:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    AdvanceDecorationFocus();
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.S when key.CtrlPressed:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    PrintBakeValues();
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }

        private void AdvanceDecorationFocus()
        {
            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);
            if (nodes.Count == 0) return;
            int idx = HabitatEnvironmentView.DecorationNode.FocusedNode == null
                ? -1
                : nodes.IndexOf(HabitatEnvironmentView.DecorationNode.FocusedNode);
            HabitatEnvironmentView.DecorationNode.FocusedNode = nodes[(idx + 1) % nodes.Count];
        }

        private static void CollectDecorationNodes(Node parent, System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode> outList)
        {
            foreach (Node child in parent.GetChildren())
            {
                if (child is HabitatEnvironmentView.DecorationNode d) outList.Add(d);
                CollectDecorationNodes(child, outList);
            }
        }

        private void UpdateScreenDebugOverlay()
        {
            var ms = FindMainScene();
            if (ms == null) return;
            if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled)
            {
                ms.SetDebugOverlayText(null);
                return;
            }

            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);

            string focused = HabitatEnvironmentView.DecorationNode.FocusedNode is { } f
                ? $"{f.Spec.PlaceholderEmoji}  scale {f.ScaleMultiplier:F1}x"
                : "(none)";

            ms.SetDebugOverlayText(
                $"DEBUG — Biome Theme Editor ({_biome})\n" +
                $"  Mode:        Decoration drag\n" +
                $"  Selected:    {focused}\n" +
                $"  Decorations: {nodes.Count}\n" +
                "  -----------------------------------\n" +
                "  Drag        : reposition\n" +
                "  O           : cycle scale (selected)\n" +
                "  Tab         : next decoration\n" +
                "  Ctrl+S      : print bake values\n" +
                "  F4 / P      : print + exit");
        }

        public void PrintBakeValues()
        {
            var theme = BiomeThemes.For(_biome);
            if (theme == null) return;

            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);

            GD.Print("");
            GD.Print($"=== BIOME THEME — {_biome.ToString().ToUpper()} ====================================");
            GD.Print($"// Paste into KeeperLegacyGodot/Data/BiomeTheme.cs:");
            GD.Print($"[HabitatType.{_biome}] = new BiomeTheme(");
            GD.Print($"    Biome:                 HabitatType.{_biome},");
            GD.Print($"    IconEmoji:             \"{theme.IconEmoji}\",");
            GD.Print($"    DisplayName:           \"{theme.DisplayName}\",");
            GD.Print($"    FlavorSubtitle:        \"{theme.FlavorSubtitle}\",");
            GD.Print($"    AccentColor:           new Color(\"{theme.AccentColor.ToHtml()}\"),");
            GD.Print($"    BackgroundTopColor:    new Color(\"{theme.BackgroundTopColor.ToHtml()}\"),");
            GD.Print($"    BackgroundBottomColor: new Color(\"{theme.BackgroundBottomColor.ToHtml()}\"),");
            GD.Print($"    Decorations: new[]");
            GD.Print($"    {{");
            foreach (var n in nodes)
            {
                float size = n.Spec.SizePx * n.ScaleMultiplier;
                string anim = n.Spec.Animation == DecorationAnimation.None
                    ? ""
                    : $", DecorationAnimation.{n.Spec.Animation}";
                GD.Print($"        new Decoration(\"{n.Spec.PlaceholderEmoji}\", new Vector2({n.BasePosition.X,5:F0}, {n.BasePosition.Y,5:F0}), {size:F0}f{anim}),");
            }
            GD.Print($"    }},");
            GD.Print($"    Particles:             /* unchanged from current theme */ null,");
            GD.Print($"    AmbientLights:         /* unchanged from current theme */ System.Array.Empty<LightShaft>(),");
            GD.Print($"    Floor:                 /* unchanged from current theme */ null,");
            GD.Print($"    Surface:               /* unchanged from current theme */ null,");
            GD.Print($"    WanderZone:            new Rect2({theme.WanderZone.Position.X}, {theme.WanderZone.Position.Y}, {theme.WanderZone.Size.X}, {theme.WanderZone.Size.Y})");
            GD.Print($");");
            GD.Print($"==============================================================");
            GD.Print("");
        }
    }
}
