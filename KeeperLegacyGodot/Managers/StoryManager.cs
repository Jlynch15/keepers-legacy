// Managers/StoryManager.cs
// Autoload singleton — wraps StoryEngine and surfaces events to the UI.
// When a story event becomes pending, it emits StoryEventPending so the
// UI can pause gameplay and show the dialogue. The UI then calls
// CompleteCurrentEvent() to advance the story.

using Godot;
using KeeperLegacy.Models;

public partial class StoryManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    /// A story event is ready to be shown to the player.
    [Signal] public delegate void StoryEventPendingEventHandler(string eventId);

    /// A story event dialogue was completed by the player.
    [Signal] public delegate void StoryEventCompletedEventHandler(string eventId);

    /// The story advanced to a new act.
    [Signal] public delegate void StoryActAdvancedEventHandler(int newAct);

    // ── State ─────────────────────────────────────────────────────────────────

    private StoryEngine _engine = new();
    private PlayerProgression? _progression;

    public PlayerStoryState StoryState => _engine.State;

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(PlayerStoryState state, PlayerProgression progression)
    {
        _engine     = new StoryEngine(state);
        _progression = progression;
    }

    // ── Trigger Check ─────────────────────────────────────────────────────────

    /// Evaluate story triggers after a game event (level-up, order, breed).
    public void CheckTriggers(
        int level           = 0,
        int fulfilledOrders = 0,
        int breedCount      = 0)
    {
        _engine.CheckTriggers(level, fulfilledOrders, breedCount);

        if (_engine.PendingEvent != null)
            EmitSignal(SignalName.StoryEventPending, _engine.PendingEvent.Id);
    }

    // ── Event Completion ──────────────────────────────────────────────────────

    /// Called by the UI when the player finishes reading the story dialogue.
    public void CompleteCurrentEvent()
    {
        if (_engine.PendingEvent is not { } evt) return;

        int prevAct = _engine.State.CurrentAct;
        _engine.CompleteCurrentEvent(_progression!, GetNode<ProgressionManager>("/root/ProgressionManager").CurrentLevel);

        EmitSignal(SignalName.StoryEventCompleted, evt.Id);

        // Re-check for act advancement
        if (_engine.State.CurrentAct != prevAct)
            EmitSignal(SignalName.StoryActAdvanced, _engine.State.CurrentAct);

        // If a new event immediately queued up, emit it
        if (_engine.PendingEvent != null)
            EmitSignal(SignalName.StoryEventPending, _engine.PendingEvent.Id);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public StoryEvent? GetPendingEvent()  => _engine.PendingEvent;
    public bool HasPendingEvent()         => _engine.PendingEvent != null;
    public int  CurrentAct()              => _engine.State.CurrentAct;

    public bool HasCompletedEvent(string eventId) =>
        _engine.State.HasCompletedEvent(eventId);
}
