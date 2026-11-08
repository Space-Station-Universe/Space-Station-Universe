using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbDamageVisualsComponent : Component
{
    [DataField(required: true)] public List<FixedPoint2> Thresholds = new();

    [DataField(required: true)] public Enum Layer;

    [DataField(required: true), AlwaysPushInheritance]
    public Dictionary<ProtoId<DamageGroupPrototype>, LimbDamageSpriteState> DamageOverlayGroups;
}

[DataDefinition]
public sealed partial class LimbDamageSpriteState
{
    [DataField(required: true)] public ResPath Rsi;
    [DataField] public Color Color = Color.White;
}