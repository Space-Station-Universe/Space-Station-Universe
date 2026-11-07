using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Speech;

namespace Content.Server._SSU.Radio;

/// <summary>
/// Raised by RadioSystem after a radio message is built, before it goes out to any radios.
/// SSU systems can cancel it or change <see cref="ChatMsg"/> for everyone (languages).
/// </summary>
[ByRefEvent]
public record struct RadioMessageStartEvent(
    EntityUid MessageSource,
    RadioChannelPrototype Channel,
    string Name,
    SpeechVerbPrototype Speech,
    string Message,
    bool EscapeMarkup,
    MsgChatMessage ChatMsg)
{
    public bool Cancelled;
}
