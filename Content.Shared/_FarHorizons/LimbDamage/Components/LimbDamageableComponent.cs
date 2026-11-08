using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbDamageableComponent : Component
{
    [DataField(required: true)] public ProtoId<LimbTargettingPrototype> Proto;
    [DataField] public ProtoId<OrganCategoryPrototype> DefaultLimb = "Torso";

    public BodyComponent? Body;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbAimedHitscanShotComponent : Component
{
    [ViewVariables] public ProtoId<OrganCategoryPrototype> Target = "Torso";
}

[ByRefEvent]
public record struct LimbHitCheckEvent(ProtoId<OrganCategoryPrototype> Target, ProtoId<OrganCategoryPrototype>? HitTarget = null, bool Handled = false);

[ByRefEvent]
public record struct LimbScatterHitTargetCheckEvent(ProtoId<OrganCategoryPrototype>? AimedTowards, ProtoId<OrganCategoryPrototype>? Target = null, bool Handled = false);