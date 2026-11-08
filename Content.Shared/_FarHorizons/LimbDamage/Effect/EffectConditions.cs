using System.Linq;
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.LimbDamage.Effect;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbConditionAnd : ILimbDamageEffectCondition
{
    [DataField] public List<ILimbDamageEffectCondition> Conditions = new();

    public bool Check(Entity<DamageableLimbComponent> limb, DamageableSystem damageable) => 
        Conditions.All(p => p.Check(limb, damageable));
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbConditionOr : ILimbDamageEffectCondition
{
    [DataField] public List<ILimbDamageEffectCondition> Conditions = new();

    public bool Check(Entity<DamageableLimbComponent> limb, DamageableSystem damageable) => 
        Conditions.Any(p => p.Check(limb, damageable));
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbConditionTotalDamage : ILimbDamageEffectCondition
{
    [DataField] public float TotalDamage;

    public bool Check(Entity<DamageableLimbComponent> limb, DamageableSystem damageable) =>
        limb.Comp.Damageable != null &&
        damageable.GetPositiveDamage((limb, limb.Comp.Damageable)).DamageDict.Sum(p => (float)p.Value) >= TotalDamage;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbConditionSpecificDamage : ILimbDamageEffectCondition
{
    [DataField] public ProtoId<DamageTypePrototype> DamageType;
    [DataField] public float DamageAmount;

    public bool Check(Entity<DamageableLimbComponent> limb, DamageableSystem damageable) =>
        limb.Comp.Damageable != null &&
        damageable.GetPositiveDamage((limb, limb.Comp.Damageable)).DamageDict
            .TryGetValue(DamageType, out var amount) && amount >= DamageAmount;
}