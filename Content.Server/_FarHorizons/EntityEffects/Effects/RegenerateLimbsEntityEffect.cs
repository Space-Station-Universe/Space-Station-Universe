using Content.Shared._FarHorizons.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Jittering;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Polymorphs this entity into another entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class RegenerateEntityEffectSystem : EntityEffectSystem<RegrowableLimbsComponent, RegenerateLimbs>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private IGameTiming _timing = default!; // SSU: regrow delay without Timer.Spawn
    [Dependency] private OrganRelationSystem _organRelation = default!; // SSU: upstream parent/child organs
    [Dependency] private LimbReattachmentandRegrowthSystem _reattach = default!; // SSU: shared limb helpers

    private static readonly TimeSpan RegrowDelay = TimeSpan.FromSeconds(5); // SSU
    private static readonly ProtoId<OrganCategoryPrototype> TorsoCategory = "Torso"; // SSU

    protected override void Effect(Entity<RegrowableLimbsComponent> ent, ref EntityEffectEvent<RegenerateLimbs> args)
    {
        // BEGIN SSU EDITS - wait with a component instead of Timer.Spawn, Update does the regrowing
        if (HasComp<RegrowingLimbsComponent>(ent) || !HasComp<InitialBodyComponent>(ent) || !HasMissingLimbs(ent))
            return;

        _jitter.DoJitter(ent.Owner, RegrowDelay, false, amplitude: 5, frequency: 10);
        EnsureComp<RegrowingLimbsComponent>(ent).RegrowAt = _timing.CurTime + RegrowDelay;
        // if(!TryComp<BodyComponent>(ent.Owner, out var bodyComp) || bodyComp.Organs == null || !TryComp<InitialBodyComponent>(ent.Owner, out var iBodyComp))
        //     return;
        //
        // _jitter.DoJitter(ent.Owner, TimeSpan.FromSeconds(5), false, amplitude: 5, frequency: 10);
        //
        // Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        // {
        //     var existingCategories = new HashSet<string>();
        //     var organData = new OrganProfileData();
        //
        //     foreach (var organId in bodyComp.Organs.ContainedEntities)
        //     {
        //         if (TryComp<OrganComponent>(organId, out var organ) && organ.Category != null
        //             && TryComp<VisualOrganComponent>(organId, out var vOrgan))
        //         {
        //             existingCategories.Add(organ.Category);
        //             if(organ.Category.ToString() == "Torso")
        //                 organData = vOrgan.Profile;
        //         }
        //     }
        //     foreach (var protoId in iBodyComp.Organs.Values)
        //     {
        //         if (!_proto.TryIndex(protoId, out var proto))
        //             continue;
        //
        //         if (!proto.TryGetComponent<OrganComponent>(out var organProto, _componentFactory))
        //             continue;
        //
        //         if (organProto.Category == null || existingCategories.Contains(organProto.Category))
        //             continue;
        //
        //         var newLimb = Spawn(protoId, Transform(ent.Owner).Coordinates);
        //         _visualBody.ApplyProfile(ent.Owner, organData);
        //         _container.Insert(newLimb, bodyComp.Organs);
        //     }
        // });
        // END SSU EDITS
    }

    // BEGIN SSU EDITS - regrow missing limbs whose parent limb is present, then link them
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RegrowingLimbsComponent, InitialBodyComponent, BodyComponent>();
        while (query.MoveNext(out var uid, out var regrowing, out var initial, out var body))
        {
            if (_timing.CurTime < regrowing.RegrowAt)
                continue;

            RemCompDeferred<RegrowingLimbsComponent>(uid);
            Regrow((uid, initial, body));
        }
    }

    private bool HasMissingLimbs(EntityUid uid)
    {
        if (!TryComp<InitialBodyComponent>(uid, out var initial) || !TryComp<BodyComponent>(uid, out var body) || body.Organs == null)
            return false;

        var existing = GetCategories(body);
        foreach (var category in initial.Organs.Keys)
        {
            if (!existing.ContainsKey(category))
                return true;
        }

        return false;
    }

    private Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> GetCategories(BodyComponent body)
    {
        var existing = new Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid>();
        foreach (var organ in body.Organs?.ContainedEntities ?? [])
        {
            if (_reattach.CategoryOf(organ) is { } category)
                existing[category] = organ;
        }

        return existing;
    }

    private void Regrow(Entity<InitialBodyComponent, BodyComponent> ent)
    {
        if (ent.Comp2.Organs == null || ent.Comp1.Relationships is not { } relationships)
            return;

        var initialOrgans = ent.Comp1.Organs;
        var existing = GetCategories(ent.Comp2);
        var profile = existing.TryGetValue(TorsoCategory, out var torso) && TryComp<VisualOrganComponent>(torso, out var visual)
            ? visual.Profile
            : new OrganProfileData();

        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var (parentCategory, children) in relationships)
            {
                if (!existing.TryGetValue(parentCategory, out var parent) || !HasComp<ParentOrganComponent>(parent))
                    continue;

                foreach (var category in children)
                {
                    if (existing.ContainsKey(category) || !initialOrgans.TryGetValue(category, out var protoId))
                        continue;

                    var limb = Spawn(protoId, Transform(ent).Coordinates);
                    if (!_container.Insert(limb, ent.Comp2.Organs))
                    {
                        Del(limb);
                        continue;
                    }

                    if (HasComp<ChildOrganComponent>(limb))
                        _organRelation.Relate(parent, limb);

                    existing[category] = limb;
                    grew = true;
                }
            }
        }

        _visualBody.ApplyProfile(ent, profile);
    }
    // END SSU EDITS
}
