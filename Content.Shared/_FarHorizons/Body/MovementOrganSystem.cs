using System.Linq;
// using Content.Shared._FarHorizons.Traits.Assorted; // SSU: uses LegsMissing instead of Far Horizons' LegsParalyzed copy
using Content.Shared.Body;
using Content.Shared.Buckle.Components; // SSU: LegsMissing
using Content.Shared.Inventory;
using Content.Shared.Movement.Components; // SSU: LegsMissing
using Content.Shared.Movement.Systems;
using Content.Shared.Standing; // SSU: LegsMissing
using Content.Shared.Stunnable; // SSU: LegsMissing

namespace Content.Shared._FarHorizons.Body;

public sealed partial class MovementOrganSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private const float NoLegsModifier = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementOrganExpectedToMoveComponent, RefreshMovementSpeedModifiersEvent>(OnMovementModifierRefresh);
        SubscribeLocalEvent<MovementOrganComponent, OrganGotRemovedEvent>((_, ref args) => RefreshModifiers(args.Target));
        SubscribeLocalEvent<MovementOrganComponent, OrganGotInsertedEvent>((_, ref args) => RefreshModifiers(args.Target));

        // BEGIN SSU EDITS - Far Horizons' LegsParalyzed copy, renamed to LegsMissing
        SubscribeLocalEvent<LegsMissingComponent, ComponentStartup>((ent, ref _) => AddCrawl(ent), after: [typeof(StandingStateSystem)]);
        SubscribeLocalEvent<LegsMissingComponent, ComponentShutdown>((ent, ref _) => RemoveCrawl(ent), after: [typeof(StandingStateSystem)]);
        SubscribeLocalEvent<LegsMissingComponent, BuckledEvent>((ent, ref _) => RemoveCrawl(ent), after: [typeof(StandingStateSystem)]);
        SubscribeLocalEvent<LegsMissingComponent, UnbuckledEvent>((ent, ref _) => AddCrawl(ent), after: [typeof(StandingStateSystem)]);
        // END SSU EDITS
    }

    private void RefreshModifiers(EntityUid target)
    {
        if (TerminatingOrDeleted(target)) return;
        _movementSpeed.RefreshMovementSpeedModifiers(target);
    }

    private void OnMovementModifierRefresh(Entity<MovementOrganExpectedToMoveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;

        if (!TryComp<BodyComponent>(ent, out var body)) return;

        if (body.Organs == null || body.Organs.Count == 0) return;

        var allLegs = body.Organs.ContainedEntities.Select(CompOrNull<MovementOrganComponent>).Where(p => p != null)
            .ToList();

        var shoesEquipped = _inventory.TryGetSlotEntity(ent, "shoes", out _);

        var walkSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.WalkSpeedModifier);
        var sprintSpeedModifier = allLegs.Sum(p => p!.ShoesNegate && shoesEquipped ? 1 : p.SprintSpeedModifier);

        var totalWalkModifier = walkSpeedModifier / ent.Comp.ExpectedAmount;
        var totalSprintModifier = sprintSpeedModifier / ent.Comp.ExpectedAmount;

        // BEGIN SSU EDITS - LegsMissing only tracks missing legs, so the wheelchair trait (upstream LegsParalyzed) needs no profile check
        if (allLegs.Count == 0)
        {
            totalWalkModifier = NoLegsModifier;
            totalSprintModifier = NoLegsModifier;
            EnsureComp<LegsMissingComponent>(ent);
        }
        else if (HasComp<LegsMissingComponent>(ent))
        {
            RemCompDeferred<LegsMissingComponent>(ent);
        }
        // if (allLegs.Count == 0)
        // {
        //     totalWalkModifier = NoLegsModifier;
        //     totalSprintModifier = NoLegsModifier;
        //     EnsureComp<LegsParalyzedComponent>(ent);
        // }
        // else if (HasComp<LegsParalyzedComponent>(ent))
        // {
        //     var shouldBeParalyzed = false;
        //
        //     if (TryComp<HumanoidCharacterProfileComponent>(ent, out var hcpComp) && hcpComp.Profile != null)
        //     {
        //         var traits = hcpComp.Profile.TraitPreferences;
        //         foreach (var trait in traits)
        //         {
        //             if (trait == "WheelchairBound")
        //             {
        //                 shouldBeParalyzed = true;
        //                 break;
        //             }
        //         }
        //     }
        //
        //     if (!shouldBeParalyzed)
        //         RemComp<LegsParalyzedComponent>(ent);
        // }
        // END SSU EDITS

        args.ModifySpeed(totalWalkModifier, totalSprintModifier);
    }

    // BEGIN SSU EDITS - Far Horizons' LegsParalyzed copy, renamed to LegsMissing
    private void RemoveCrawl(EntityUid ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<WormComponent>(ent);
        RemCompDeferred<KnockedDownComponent>(ent);
    }

    private void AddCrawl(EntityUid ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        EnsureComp<WormComponent>(ent);
        EnsureComp<KnockedDownComponent>(ent);
    }
    // END SSU EDITS
}
