using Content.Shared.Chat;

namespace Content.Server._SSU.Chat;

[ByRefEvent]
public record struct InGameICMessageStartEvent(EntityUid Source, string Message, InGameICChatType DesiredType)
{
    public bool Cancelled;
    public IDisposable? Scope;
}
