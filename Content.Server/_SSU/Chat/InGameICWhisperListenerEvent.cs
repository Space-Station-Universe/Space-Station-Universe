namespace Content.Server._SSU.Chat;

[ByRefEvent]
public record struct InGameICWhisperListenerEvent(
    EntityUid Source,
    EntityUid Listener,
    string Name,
    string NameIdentity,
    string Message,
    string WrappedMessage,
    string MuffledMessage,
    string MuffledWrappedMessage,
    string UnknownWrappedMessage);
