using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Body;

[RegisterComponent, NetworkedComponent]
public sealed partial class VisionOrganRequiredForVisionComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VisionOrganComponent : Component
{
    public int EyeDamage;
    public int MinDamage;
}

// SSU: surgery only, port with phase 3
// [RegisterComponent, NetworkedComponent]
// public sealed partial class CustomOrganComponent : Component;

// SSU: unused in YAML/code, not ported
// [RegisterComponent, NetworkedComponent]
// public sealed partial class HeadOrganComponent : Component
// {
//     public string NameBackup = "";
// }

// SSU: brain transfer, port with phase 3
// [RegisterComponent, NetworkedComponent]
// public sealed partial class BrainExtraComponent : Component
// {
//     [DataField]
//     public ComponentRegistry StoredComponents = new();
// }

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementOrganExpectedToMoveComponent : Component
{
    [DataField] public int ExpectedAmount = 2;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementOrganComponent : Component
{
    [DataField] public float WalkSpeedModifier = 1;
    [DataField] public float SprintSpeedModifier = 1;
    [DataField] public bool ShoesNegate;
}

// SSU: replaced by upstream ParentOrgan/ChildOrgan
// [RegisterComponent, NetworkedComponent]
// public sealed partial class ConnectedOrganComponent : Component
// {
//     public const string ContainerID = "connected_organs";
//
//     [ViewVariables] public Container? Organs;
//
//     [DataField] public List<EntProtoId> Roundstart = new List<EntProtoId>();
// }

[RegisterComponent, NetworkedComponent]
public sealed partial class VitalOrganComponent : Component
{
    [DataField] public DamageSpecifier Damage = new()
    {
        DamageDict = new ()
        {
            { "Bloodloss", 125 },
            { "Asphyxiation", 75}    // Just enough to kill
        }
    };
}

// SSU: upstream DetachedBody draws detached limbs, not ported
// [RegisterComponent]
// public sealed partial class DetachedOrganVisualsExcludedComponent : Component;

[RegisterComponent]
public sealed partial class ReattacheableLimbsComponent : Component
{
    [DataField]
    public TimeSpan ReattachTime = TimeSpan.FromSeconds(5);
}

[RegisterComponent]
public sealed partial class ReattacheableLimbComponent : Component;

[Serializable, NetSerializable]
public sealed partial class ReattachmentAttemptEvent : SimpleDoAfterEvent;

[RegisterComponent]
public sealed partial class RegrowableLimbsComponent : Component;

// BEGIN SSU EDITS - regrowth waits on a component instead of Timer.Spawn
[RegisterComponent]
public sealed partial class RegrowingLimbsComponent : Component
{
    [ViewVariables] public TimeSpan RegrowAt;
}

// Far Horizons' LegsParalyzed copy, renamed so it doesn't clash with upstream's LegsParalyzed trait.
[RegisterComponent, NetworkedComponent]
public sealed partial class LegsMissingComponent : Component;
// END SSU EDITS

// SSU: upstream DetachedBody draws detached limbs, not ported
// [ByRefEvent]
// public readonly record struct OnDisconnectedVisualOrganState;
// [ByRefEvent]
// public readonly record struct OnDisconnectedVisualMarkingsOrganState;

// SSU: brain transfer, port with phase 3
// [ByRefEvent]
// public readonly record struct BrainInserted(EntityUid Body);
// [ByRefEvent]
// public readonly record struct BrainRemoved(EntityUid Body);
