using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.LimbDamage.Effect;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbDamageEffect
{
    [DataField] public bool TriggerOnHeal;

    [DataField] public ILimbDamageEffectCondition? Condition;
    [DataField] public ILimbDamageEffect? Effect;
}

public interface ILimbDamageEffectCondition
{
    public bool Check(Entity<DamageableLimbComponent> limb, DamageableSystem damageable);
}

public interface ILimbDamageEffect
{
    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan);
}