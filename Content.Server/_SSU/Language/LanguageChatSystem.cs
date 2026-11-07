using Content.Server._SSU.Chat;
using Content.Server._Starlight.Language;
using Content.Server.Popups;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Popups;

namespace Content.Server._SSU.Language;

public sealed partial class LanguageChatSystem : EntitySystem
{
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private PopupSystem _popup = default!;

    private readonly Stack<LanguagePrototype> _active = new();

    public LanguagePrototype? CurrentLanguage => _active.Count > 0 ? _active.Peek() : null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InGameICMessageStartEvent>(OnICMessageStart);
    }

    public LanguagePrototype GetMessageLanguage(EntityUid source)
    {
        return CurrentLanguage ?? _language.GetLanguage(source);
    }

    public IDisposable PushLanguage(LanguagePrototype language)
    {
        _active.Push(language);
        return new LanguageScope(this);
    }

    private void OnICMessageStart(ref InGameICMessageStartEvent args)
    {
        var message = args.Message;

        var language = message.StartsWith(SharedLanguageSystem.ChatPrefixChar)
            ? _language.GetLanguageFromPrefix(args.Source, ref message, out _, true)
            : GetMessageLanguage(args.Source);

        if (language.SpeechOverride.RequireHands && !_actionBlocker.CanInteract(args.Source, null))
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-language-requires-hands"), args.Source, args.Source, PopupType.Medium);
            args.Cancelled = true;
            return;
        }

        if (language.SpeechOverride.ChatTypeOverride is { } chatType)
            args.DesiredType = chatType;

        args.Message = message;
        args.Scope = PushLanguage(language);
    }

    private sealed class LanguageScope(LanguageChatSystem owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner._active.Pop();
        }
    }
}
