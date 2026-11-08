using Content.Shared._SSU.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server._SSU.Language;

/// <summary>
/// Lets us add languages to species instead of directly editing their yaml.
/// Soooo much better and modular too. Except for half of the code being in _starlight lul
/// </summary>
public sealed partial class SpeciesLanguageSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<HumanoidProfileComponent> ent, ref MapInitEvent args)
    {
        if (HasComp<LanguageKnowledgeComponent>(ent))
            return;

        if (!_prototype.TryIndex<SpeciesLanguagesPrototype>(ent.Comp.Species.Id, out var languages))
            return;

        var knowledge = AddComp<LanguageKnowledgeComponent>(ent);
        knowledge.SpokenLanguages = new(languages.Speaks);
        knowledge.UnderstoodLanguages = new(languages.Understands);

        // Without a speaker the mob counts as only knowing Universal.
        var speaker = EnsureComp<LanguageSpeakerComponent>(ent);
        _language.UpdateEntityLanguages((ent, speaker));
    }
}
