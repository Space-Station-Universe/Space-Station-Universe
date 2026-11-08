using System.Linq;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes; // SSU: organ categories

namespace Content.Shared._FarHorizons.Body;

public sealed partial class LimbReattachmentandRegrowthSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OrganRelationSystem _organRelation = default!; // SSU: upstream parent/child organs
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReattacheableLimbsComponent, InteractUsingEvent>(OnInteract);
        // SubscribeLocalEvent<ReattacheableLimbComponent, ReattachmentAttemptEvent>(OnReattachAttempt); // SSU: the used item is the detached body, not the limb
        SubscribeLocalEvent<BodyComponent, ReattachmentAttemptEvent>(OnReattachAttempt); // SSU
    }

    private void OnInteract(Entity<ReattacheableLimbsComponent> ent, ref InteractUsingEvent args)
    {
        // BEGIN SSU EDITS - upstream detaches a limb with its children into a DetachedBody, reattach that whole piece
        if (args.Handled || args.Used == ent.Owner || HasComp<ReattacheableLimbsComponent>(args.Used))
            return;

        if (GetRootLimb(args.Used) is not { } root || !HasComp<ReattacheableLimbComponent>(root))
            return;

        if (FindParentOrgan(ent.Owner, root) != null)
        {
            var ev = new ReattachmentAttemptEvent();
            var installDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.ReattachTime, ev, args.Used, ent.Owner, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true
            };
            _doAfter.TryStartDoAfter(installDoAfter);
        }
        else
            _popupSystem.PopupClient("Can't reattach this limb.", args.User, args.User);

        args.Handled = true;
        // if(args.Handled
        //     || !HasComp<ReattacheableLimbComponent>(args.Used) || !HasComp<VisualOrganComponent>(args.Used)
        //     || !TryComp<BodyComponent>(ent.Owner, out var body) || body.Organs == null || !TryComp<OrganComponent>(args.Used, out var organComp1))
        //         return;
        //
        // var attachable = !body.Organs.ContainedEntities.ToArray().Any(organId =>
        //     TryComp<OrganComponent>(organId, out var organComp2) &&
        //     organComp2.Category == organComp1.Category);
        //
        // if(attachable)
        // {
        //     var ev = new ReattachmentAttemptEvent();
        //     var installDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.ReattachTime, ev, args.Used, ent.Owner)
        //     {
        //         BreakOnDamage = true,
        //         BreakOnMove = true,
        //         BreakOnWeightlessMove = true
        //     };
        //     _doAfter.TryStartDoAfter(installDoAfter);
        // }
        // else
        //     _popupSystem.PopupCursor("Can't reattach this limb.");
        //
        // args.Handled = true;
        // END SSU EDITS
    }

    private void OnReattachAttempt(Entity<BodyComponent> ent, ref ReattachmentAttemptEvent args) // SSU: was the limb, now the detached body
    {
        // BEGIN SSU EDITS - move every organ of the detached piece back and link its root to the parent limb
        if (args.Handled || args.Cancelled || args.Target is not { } target ||
            !TryComp<BodyComponent>(target, out var body) || body.Organs == null ||
            GetRootLimb(ent.Owner) is not { } root ||
            FindParentOrgan(target, root) is not { } parent)
            return;

        args.Handled = true;

        foreach (var organ in ent.Comp.Organs?.ContainedEntities.ToArray() ?? [])
            _container.Insert(organ, body.Organs);

        _organRelation.Relate(parent, root);

        if (ent.Comp.Organs == null || ent.Comp.Organs.Count == 0)
            PredictedQueueDel(ent.Owner);

        _popupSystem.PopupClient("You reattached this limb successfully.", args.User, args.User);
        // if(!TryComp<BodyComponent>(args.Target, out var body) || body.Organs == null)
        //     return;
        // _container.Insert(uid, body.Organs);
        // _popupSystem.PopupCursor("You reattached this limb successfully.");
        // END SSU EDITS
    }

    // BEGIN SSU EDITS - helpers for upstream parent/child organs
    public EntityUid? GetRootLimb(EntityUid detached)
    {
        if (!TryComp<BodyComponent>(detached, out var body) || body.Organs == null || HasComp<InitialBodyComponent>(detached))
            return null;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<ChildOrganComponent>(organ, out var child) && child.Parent == null)
                return organ;
        }

        return null;
    }

    public EntityUid? FindParentOrgan(Entity<InitialBodyComponent?, BodyComponent?> target, EntityUid limb)
    {
        if (!Resolve(target, ref target.Comp1, ref target.Comp2, false) || target.Comp2.Organs == null ||
            !TryComp<OrganComponent>(limb, out var limbOrgan) || limbOrgan.Category is not { } category)
            return null;

        var organs = target.Comp2.Organs.ContainedEntities;
        if (organs.Any(o => CategoryOf(o) == category))
            return null;

        var relationships = target.Comp1.Relationships;
        if (relationships == null)
            return null;

        foreach (var (parentCategory, children) in relationships)
        {
            if (!children.Contains(category))
                continue;

            foreach (var organ in organs)
            {
                if (CategoryOf(organ) == parentCategory && HasComp<ParentOrganComponent>(organ))
                    return organ;
            }
        }

        return null;
    }

    public ProtoId<OrganCategoryPrototype>? CategoryOf(EntityUid organ)
    {
        return TryComp<OrganComponent>(organ, out var comp) ? comp.Category : null;
    }
    // END SSU EDITS
}
