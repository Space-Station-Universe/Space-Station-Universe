using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbArmorComponent : Component
{
    [DataField] public Dictionary<ProtoId<OrganCategoryPrototype>, DamageModifierSet> Limbs = new();
}

[ByRefEvent]
public record struct LimbDamageModifyEvent(DamageSpecifier Damage, ProtoId<OrganCategoryPrototype> Target, float ArmorPenetration = 0f, bool CanHeal = false)
    : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.All;
}