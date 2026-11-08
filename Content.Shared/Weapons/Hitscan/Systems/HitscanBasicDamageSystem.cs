using Content.Shared._FarHorizons.LimbDamage; // SSU: Far Horizons limb damage
using Content.Shared.Body; // ditto
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Prototypes; // SSU: Far Horizons limb damage

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!; // SSU: Far Horizons limb damage

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null
            || args.DamageHandled) // SSU: Far Horizons limb damage
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // BEGIN SSU EDITS - Far Horizons limb damage, untargetted shots can scatter onto a limb
        ProtoId<OrganCategoryPrototype>? limbTarget = null;
        if (args.Data.Shooter != null)
            limbTarget = _limbDamage.TryScatterHitTarget(args.Data.HitEntity.Value, args.Data.Shooter.Value);

        // Falls back to normal body damage when there is no limb to hit.
        if ((limbTarget == null
             || !_limbDamage.TryChangeLimbDamage(args.Data.HitEntity.Value, limbTarget.Value, dmg, out var damageDealt, origin: args.Data.Shooter))
            && !_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out damageDealt, origin: args.Data.Gun))
            return;
        // END SSU EDITS

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
        };

        RaiseLocalEvent(ent, ref damageEvent);

        args.DamageHandled = true; // SSU: Far Horizons limb damage
    }
}
