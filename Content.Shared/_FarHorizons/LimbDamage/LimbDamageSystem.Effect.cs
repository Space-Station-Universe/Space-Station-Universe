using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{
    private void InitEffect()
    {
        SubscribeLocalEvent<LimbDamageEffectComponent, ComponentInit>(OnEffectInit);
        SubscribeLocalEvent<LimbDamageEffectComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnEffectInit(Entity<LimbDamageEffectComponent> ent, ref ComponentInit args) =>
        ent.Comp.DamageableLimb = EnsureComp<DamageableLimbComponent>(ent);

    private void OnDamageChanged(Entity<LimbDamageEffectComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty || !_timing.IsFirstTimePredicted)
            return;

        foreach (var effect in ent.Comp.Effects)
        {
            if ((!args.DamageIncreased && !effect.TriggerOnHeal) ||
                effect.Condition == null || ent.Comp.DamageableLimb == null ||
                !effect.Condition.Check((ent.Owner, ent.Comp.DamageableLimb), _damageable))
                continue;

            effect.Effect?.Run((ent.Owner, ent.Comp.DamageableLimb), EntityManager, _protoMan);
        }
    }
}