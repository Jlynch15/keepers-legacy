// UI/Habitat/HabitatPalette.cs
// Centralized colors for the Habitat Category screen. Per project memory
// "Palette Rework Pending", every color used on this screen lives here so the
// future palette swap is a single-file edit.

using Godot;

namespace KeeperLegacy.UI.Habitat
{
    public static class HabitatPalette
    {
        public static readonly Color OverlayBarBg          = new Color(0.047f, 0.035f, 0.027f, 0.80f);
        public static readonly Color OverlayBarBorderTint  = new Color(1.00f, 1.00f, 1.00f, 0.15f); // tinted by biome at runtime
        public static readonly Color BackButtonText        = new Color(0.91f, 0.72f, 0.19f, 0.75f);
        public static readonly Color BackButtonHover       = new Color(0.94f, 0.80f, 0.31f, 1.00f);
        public static readonly Color SeparatorLine         = new Color(0.227f, 0.157f, 0.094f, 0.80f);
        public static readonly Color CoinsText             = new Color(0.94f, 0.80f, 0.31f, 1.00f);
        public static readonly Color CoinsBg               = new Color(0.91f, 0.72f, 0.19f, 0.08f);

        public static readonly Color RosterPanelBg         = new Color(0.102f, 0.071f, 0.031f, 1.00f);
        public static readonly Color RosterCapacityBorder  = new Color(1.00f, 1.00f, 1.00f, 0.30f);
        public static readonly Color SlotBgIdle            = new Color(1.00f, 1.00f, 1.00f, 0.05f);
        public static readonly Color SlotBgSelected        = new Color(1.00f, 1.00f, 1.00f, 0.15f);
        public static readonly Color SlotEmptyBorder       = new Color(1.00f, 1.00f, 1.00f, 0.15f);

        public static readonly Color LabelName             = new Color(0.94f, 0.91f, 0.85f, 1.00f);
        public static readonly Color LabelMuted            = new Color(0.60f, 0.50f, 0.44f, 1.00f);
        public static readonly Color LabelLocked           = new Color(0.38f, 0.38f, 0.38f, 1.00f);

        public static readonly Color ChoiceMenuBg          = new Color(0.10f, 0.08f, 0.06f, 0.95f);
        public static readonly Color ChoiceMenuBorder      = new Color(1.00f, 1.00f, 1.00f, 0.20f);
        public static readonly Color ConfirmDialogScrim    = new Color(0.00f, 0.00f, 0.00f, 0.55f);
    }
}
