using Content.Shared.Chat;

namespace Content.Server._SSU.Radio;

/// <summary>
/// Raised once per player when a headset or intrinsic radio sends them a radio message.
/// SSU systems can swap <see cref="ChatMsg"/> for that one player (scrambled languages).
/// </summary>
[ByRefEvent]
public record struct RadioListenerMessageEvent(EntityUid Listener, MsgChatMessage ChatMsg);
