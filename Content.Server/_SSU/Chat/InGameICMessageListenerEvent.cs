using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server._SSU.Chat;

/// <summary>
/// Raised once per listener when ChatSystem sends a message in voice range.
/// Lets SSU systems change what that one listener receives (scrambled languages).
/// <see cref="Name"/> and <see cref="Speech"/> are only set for normal speech.
/// </summary>
[ByRefEvent]
public record struct InGameICMessageListenerEvent(
    EntityUid Source,
    EntityUid Listener,
    ChatChannel Channel,
    string? Name,
    SpeechVerbPrototype? Speech,
    string Message,
    string WrappedMessage);
