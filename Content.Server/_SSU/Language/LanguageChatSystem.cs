using Content.Server._SSU.Chat;
using Content.Server._Starlight.Language;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._SSU.Language;

/// <summary>
///  Connects Starlight's languages to upstream ChatSystem through small SSU event hooks, instead of editing ChatSystem directly like Starlight does.
/// </summary>
public sealed partial class LanguageChatSystem : EntitySystem
{
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// Same as Starlight's ChatSystem.DefaultSpeakColor.
    public static readonly Color DefaultSpeakColor = Color.LightGray;

    private readonly Stack<LanguageScope> _active = new();

    public LanguagePrototype? CurrentLanguage => _active.Count > 0 ? _active.Peek().Language : null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InGameICMessageStartEvent>(OnICMessageStart);
        SubscribeLocalEvent<InGameICMessageListenerEvent>(OnICMessageListener);
    }

    public LanguagePrototype GetMessageLanguage(EntityUid source)
    {
        return CurrentLanguage ?? _language.GetLanguage(source);
    }

    public IDisposable PushLanguage(LanguagePrototype language)
    {
        var scope = new LanguageScope(this, language);
        _active.Push(scope);
        return scope;
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

    /// <summary>
    ///     Gives each listener either the understood or the scrambled version of the message.
    ///     Only normal speech is handled here, emotes and LOOC don't pass a name and are skipped.
    /// </summary>
    private void OnICMessageListener(ref InGameICMessageListenerEvent args)
    {
        if (args.Name is not { } name || args.Speech is not { } speech)
            return;

        if (!_active.TryPeek(out var scope))
            return;

        // Each scope is one IC message, reset the cache if a different message comes through.
        if (scope.CachedSource != args.Source || scope.CachedMessage != args.Message)
        {
            scope.CachedSource = args.Source;
            scope.CachedMessage = args.Message;
            scope.Understood = null;
            scope.Obfuscated = null;
        }

        var language = scope.Language;
        if (_language.CanUnderstand(args.Listener, language.ID))
        {
            // Languages with no visual changes keep the normal upstream message.
            if (!ChangesUnderstoodWrap(language))
                return;

            scope.Understood ??= (args.Message, WrapSpeech(name, speech, args.Message, language, false));
            (args.Message, args.WrappedMessage) = scope.Understood.Value;
            return;
        }

        if (scope.Obfuscated == null)
        {
            var obfuscated = ObfuscateMessage(args.Message, language);
            scope.Obfuscated = (obfuscated, WrapSpeech(name, speech, obfuscated, language, true));
        }

        (args.Message, args.WrappedMessage) = scope.Obfuscated.Value;
    }

    private static bool ChangesUnderstoodWrap(LanguagePrototype language)
    {
        var over = language.SpeechOverride;
        return language.IconVisibleIfUnderstood
            || over.Color != null
            || over.FontId != null && over.ObfuscationFont != true
            || over.FontSize != null
            || over.SpeechVerbOverrides != null
            || over.MessageWrapOverrides.ContainsKey(InGameICChatType.Speak);
    }

    /// <summary>
    /// Port of Starlight's ObfuscateSpeech + SanitizeInGameICMessage for the scrambled message.
    /// </summary>
    private string ObfuscateMessage(string message, LanguagePrototype language)
    {
        var obfuscated = _chat.SanitizeMessageCapital(_language.ObfuscateSpeech(message, language));

        if (_cfg.GetCVar(CCVars.ChatPunctuation) && obfuscated.Length > 0 && char.IsLetter(obfuscated[^1]))
            obfuscated += ".";

        return obfuscated;
    }

    /// <summary>
    /// Port of Starlight's ChatSystem.WrapMessage for speech, using SSU's name links.
    /// </summary>
    private string WrapSpeech(string name, SpeechVerbPrototype speech, string message, LanguagePrototype language, bool obfuscated)
    {
        var over = language.SpeechOverride;

        // Upstream wrap strings have no color, so only use the SSU colored ones when the language has a color.
        var bold = speech.Bold && !(obfuscated && over.ObfuscationFont == true);
        LocId wrapId;
        if (over.MessageWrapOverrides.TryGetValue(InGameICChatType.Speak, out var wrapOverride))
            wrapId = wrapOverride;
        else if (over.Color != null)
            wrapId = bold ? "ssu-language-say-bold-wrap-message" : "ssu-language-say-wrap-message";
        else
            wrapId = bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message";

        var verbId = over.SpeechVerbOverrides is { Count: > 0 } verbs
            ? _random.Pick(verbs).ToString()
            : _random.Pick(speech.SpeechVerbStrings);

        var color = DefaultSpeakColor;
        if (over.Color is { } colorOverride)
            color = Color.InterpolateBetween(color, colorOverride, colorOverride.A);

        var nameString = _language.GetLanguageIcon(language, obfuscated)
            ? $"[icon src=\"{language.Icon}\" tooltip=\"{language.Name}\"] {name}"
            : name;

        var fontType = over.FontId ?? speech.FontId;
        if (over.ObfuscationFont == true && !obfuscated)
            fontType = speech.FontId;

        return Loc.GetString(wrapId,
            ("color", color),
            ("entityName", nameString),
            ("verb", Loc.GetString(verbId)),
            ("fontType", fontType),
            ("fontSize", over.FontSize ?? speech.FontSize),
            ("message", FormattedMessage.EscapeText(message)));
    }

    private sealed class LanguageScope(LanguageChatSystem owner, LanguagePrototype language) : IDisposable
    {
        public readonly LanguagePrototype Language = language;

        // Per-message cache so every listener gets the same text and we only build it once.
        public EntityUid? CachedSource;
        public string? CachedMessage;
        public (string Message, string Wrapped)? Understood;
        public (string Message, string Wrapped)? Obfuscated;

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
