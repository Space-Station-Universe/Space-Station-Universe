using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageableLimbComponent : Component
{
    [DataField] public float BodyDamageFactor;
    [DataField] public float MissChance;
    [DataField] public float RedirectChance;
    [DataField] public ProtoId<OrganCategoryPrototype> RedirectTarget = "Torso";

    [DataField] public float ScatterHitChance;
    [DataField] public float AimedTowardsScatterHitChance;

    public DamageableComponent? Damageable;
    public OrganComponent? Organ;
}