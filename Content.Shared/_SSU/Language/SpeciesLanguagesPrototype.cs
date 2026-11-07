using Content.Shared._Starlight.Language;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._SSU.Language;

/// <summary>
/// Lets us add languages to species instead of directly editing their yaml.
/// Soooo much better and modular too. Except for half of the code being in _starlight lul
/// </summary>
[Prototype]
public sealed partial class SpeciesLanguagesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<LanguagePrototype>> Speaks = new();

    [DataField(required: true)]
    public List<ProtoId<LanguagePrototype>> Understands = new();
}
