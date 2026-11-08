using Content.Server.Mind;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Species.Components;
using Content.Shared.Whitelist;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using System.Linq; // SSU: Far Horizons brain transfer

namespace Content.Server.Species.Systems;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private ZombieSystem _zombie = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, OrganGotRemovedEvent>(OnRemovedFromPart);
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.Target))
            return;

        if (!ProtoMan.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        if (!_whitelist.CheckBoth(args.Target, comp.Blacklist, comp.Whitelist))
            return;

        // Get the organs' position & spawn a nymph there
        var coords = Transform(uid).Coordinates;
        var nymph = SpawnAtPosition(entityProto.ID, coords);

        if (HasComp<ZombieComponent>(args.Target)) // Zombify the new nymph if old one is a zombie
            _zombie.ZombifyEntity(nymph);

        // Move the mind if there is one and it's supposed to be transferred
        if (comp.TransferMind && _mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            // BEGIN SSU EDITS - Far Horizons brain transfer, the nymph's brain keeps the stored components
            if (TryComp<BodyComponent>(nymph, out var body) && body.Organs != null &&
                body.Organs.ContainedEntities.FirstOrDefault(HasComp<Content.Shared.Body.Components.BrainComponent>) is { Valid: true } nymphBrain &&
                TryComp<Content.Shared._FarHorizons.Body.BrainExtraComponent>(uid, out var brainComp))
                EnsureComp<Content.Shared._FarHorizons.Body.BrainExtraComponent>(nymphBrain).StoredComponents = brainComp.StoredComponents;
            // END SSU EDITS
            _mindSystem.TransferTo(mindId, nymph, mind: mind);
        }

        // Delete the old organ
        QueueDel(uid);
    }
}
