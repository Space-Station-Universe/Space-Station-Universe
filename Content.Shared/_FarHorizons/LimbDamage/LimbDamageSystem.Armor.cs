using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;

namespace Content.Shared._FarHorizons.LimbDamage;

public partial class LimbDamageSystem
{

    private void InitArmor()
    {
        SubscribeLocalEvent<DamageableLimbComponent, DamageModifyEvent>(OnLimbDamaged);
        SubscribeLocalEvent<LimbArmorComponent, InventoryRelayedEvent<LimbDamageModifyEvent>>(OnLimbArmorProtect);
    }

    private void OnLimbArmorProtect(Entity<LimbArmorComponent> ent, ref InventoryRelayedEvent<LimbDamageModifyEvent> args)
    {
        if (!ent.Comp.Limbs.TryGetValue(args.Args.Target, out var modifiers)) return;

        if (modifiers.Coefficients.Count == 0 && modifiers.FlatReductions.Count == 0 /* SSU: upstream renamed FlatReduction */)
        {
            if (!TryComp<ArmorComponent>(ent, out var armor)) return;
            modifiers = armor.Modifiers;
        }

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, modifiers); // SSU: upstream ApplyModifierSet has no armor penetration/canHeal
    }

    private void OnLimbDamaged(Entity<DamageableLimbComponent> ent, ref DamageModifyEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Category == null || organ.Body == null) return;

        var ev = new LimbDamageModifyEvent(args.Damage, organ.Category.Value); // SSU: upstream DamageModifyEvent has no ArmorPenetration/CanHeal
        RaiseLocalEvent(organ.Body.Value, ref ev);
        args.Damage = ev.Damage;
    }
}