using Content.Shared.Body; // SSU: Far Horizons limb damage
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; // SSU: Far Horizons limb damage

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem))]
public sealed partial class TargetedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    [DataField, AutoNetworkedField] public ProtoId<OrganCategoryPrototype>? LimbTarget; // SSU: Far Horizons limb damage
}
