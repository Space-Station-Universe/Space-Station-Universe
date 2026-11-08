using Content.Shared.Body; // SSU: Far Horizons limb damage
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes; // SSU: Far Horizons limb damage
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent
{
    // BEGIN SSU EDITS - Far Horizons limb damage
    public ProtoId<OrganCategoryPrototype>? TargettedLimb;

    public HealingDoAfterEvent(ProtoId<OrganCategoryPrototype>? limb = null) => TargettedLimb = limb;
    // END SSU EDITS
}
