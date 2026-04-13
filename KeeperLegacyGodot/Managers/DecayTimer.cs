// Managers/DecayTimer.cs
// Child node of GameManager (NOT an autoload).
// Fires every 30 real-time minutes. On each tick:
//   - Applies stat decay to all owned creatures
//   - Advances creature lifecycles
//   - Prunes expired orders
//   - Triggers an auto-save
//
// The tick interval is 1800 seconds (30 min) in production.
// Use DECAY_INTERVAL_SECONDS = 60 during local testing if needed.

using Godot;

public partial class DecayTimer : Node
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// Seconds between decay ticks. 1800 = 30 minutes.
    private const float DECAY_INTERVAL_SECONDS = 1800f;

    /// Hours of game time that pass per tick (matches the real-time interval).
    private const double DECAY_HOURS_PER_TICK = 0.5;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var timer = new Timer();
        timer.WaitTime  = DECAY_INTERVAL_SECONDS;
        timer.Autostart = true;
        timer.OneShot   = false;
        timer.Timeout  += OnTick;
        AddChild(timer);
    }

    // ── Tick ──────────────────────────────────────────────────────────────────

    private void OnTick()
    {
        var habitat = GetNode<HabitatManager>("/root/HabitatManager");
        var orders  = GetNode<OrderManager>("/root/OrderManager");
        var game    = GetNode<GameManager>("/root/GameManager");

        habitat.ApplyDecay(DECAY_HOURS_PER_TICK);
        habitat.TickLifecycles();
        orders.PruneExpired();
        game.SaveGame();
    }
}
