using System.Linq;
// using Content.Shared._FarHorizons.Body; // SSU: upstream ParentOrgan/ChildOrgan replace ConnectedOrgan
using Content.Shared._FarHorizons.LimbDamage.Components;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.LimbDamage.Effect;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectDetach : ILimbDamageEffect
{
    private static readonly SoundSpecifier? _gibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (limb.Comp.Organ?.Body == null)
            return;

        var body = limb.Comp.Organ.Body.Value;

        // SSU: upstream detaching moves the limb and its children into a new DetachedBody
        if (entMan.HasComponent<DetachableOrganComponent>(limb))
            entMan.System<DetachableOrganSystem>().Detach(limb.Owner);
        else
            entMan.System<SharedContainerSystem>().TryRemoveFromContainer(limb.Owner);
        entMan.System<SharedAudioSystem>().PlayPredicted(_gibSound, body, null);
    }
        
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectDestroy : ILimbDamageEffect
{
    private static readonly SoundSpecifier? _gibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        // BEGIN SSU EDITS - upstream ParentOrgan/ChildOrgan replace ConnectedOrgan
        // Children (hands, feet, ...) drop off as their own detached bodies, then the limb is deleted.
        var body = limb.Comp.Organ?.Body;

        if (entMan.TryGetComponent<ParentOrganComponent>(limb, out var parent))
            DropAllConnected(parent.Children.ToList(), entMan);

        MakeNoiseAndDelete(limb, entMan);

        // A detached body left with nothing in it has nothing left to show.
        if (body != null &&
            !entMan.HasComponent<LimbDamageableComponent>(body) &&
            entMan.TryGetComponent<BodyComponent>(body, out var bodyComp) &&
            bodyComp.Organs is { } organs &&
            organs.ContainedEntities.All(p => p == limb.Owner))
            entMan.QueueDeleteEntity(body.Value);
        // END SSU EDITS

        /* SSU: Far Horizons ConnectedOrgan version
        if (limb.Comp.Organ?.Body == null ||
            !entMan.TryGetComponent<BodyComponent>(limb.Comp.Organ.Body.Value, out var body))
            DestroyLimbOutsideOfBody(limb, entMan, protoMan);
        else
            DestroyLimbInBody(limb, entMan, protoMan, body);
        */
    }

    /* SSU: Far Horizons ConnectedOrgan version

    private void DestroyLimbOutsideOfBody(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (!entMan.TryGetComponent<ConnectedOrganComponent>(limb, out var connectedOrgan) ||
            connectedOrgan.Organs == null)
            MakeNoiseAndDelete(limb, entMan);
        else
        {
            var connectedOrgans = new List<EntityUid>(connectedOrgan.Organs.ContainedEntities); // A copy because we're about to modify original
        
            DropAllConnected(connectedOrgans, entMan);
            MakeNoiseAndDelete(limb, entMan);
        }
    }

    private static void DestroyLimbInBody(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan, BodyComponent body)
    {
        var connectedOrgans = body.Organs?.ContainedEntities.Where(p =>
            entMan.TryGetComponent<OrganComponent>(p, out var organ) &&
            protoMan.Index(organ.Category)?.ConnectsTo == limb.Comp.Organ!.Category).ToList();

        DropAllConnected(connectedOrgans, entMan);
        MakeNoiseAndDelete(limb, entMan);
    }

    */

    private static void DropAllConnected(List<EntityUid>? organs, IEntityManager entMan)
    {
        if (organs == null || organs.Count == 0) return;

        var container = entMan.System<SharedContainerSystem>();
        var detachable = entMan.System<DetachableOrganSystem>(); // SSU: upstream detaching

        foreach (var organ in organs)
        {
            // SSU: detach with upstream when possible so children come along
            if (entMan.HasComponent<DetachableOrganComponent>(organ))
                detachable.Detach(organ);
            else
                container.TryRemoveFromContainer(organ);
        }
    }

    private static void MakeNoiseAndDelete(EntityUid target, IEntityManager entMan)
    {
        entMan.System<SharedAudioSystem>().PlayPredicted(_gibSound, target, null);
        entMan.QueueDeleteEntity(target);
    }
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectDisarm : ILimbDamageEffect
{
    [DataField] public string HandId = "";

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (limb.Comp.Organ?.Body == null) return;
        entMan.System<SharedHandsSystem>().TryDrop(limb.Comp.Organ.Body.Value, HandId, checkActionBlocker: false);
    }
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LimbEffectKnockdown : ILimbDamageEffect
{
    [DataField] public float KnockdownTime;

    public void Run(Entity<DamageableLimbComponent> limb, IEntityManager entMan, IPrototypeManager protoMan)
    {
        if (limb.Comp.Organ?.Body == null) return;
        entMan.System<SharedStunSystem>().TryKnockdown(limb.Comp.Organ.Body.Value, TimeSpan.FromSeconds(KnockdownTime), false, true, false);
    }
}