using Content.Shared._FarHorizons.LimbDamage; // SSU: Far Horizons limb damage
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage; // SSU: Far Horizons limb damage
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Content.Shared.Body; // SSU: Far Horizons limb damage
using Robust.Shared.Prototypes; // ditto

namespace Content.Shared.Medical.Healing;

public sealed partial class HealingSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedStackSystem _stacks = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private LimbDamageSystem _limbDamage = default!; // SSU: Far Horizons limb damage

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DamageableComponent> target, ref HealingDoAfterEvent args)
    {

        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp(args.Used, out HealingComponent? healing))
            return;

        if (!TryComp<InjurableComponent>(target, out var injurable))
            return;

        if (healing.DamageContainers is not null &&
            injurable.DamageContainer is not null &&
            !healing.DamageContainers.Contains(injurable.DamageContainer.Value))
        {
            return;
        }

        TryComp<BloodstreamComponent>(target, out var bloodstream);

        // Heal some bloodloss damage.
        if (healing.BloodlossModifier != 0 && bloodstream != null)
        {
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount((target.Owner, bloodstream), healing.BloodlossModifier);
            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                var popup = (args.User == target.Owner)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(target.Owner, EntityManager)));
                _popupSystem.PopupEntity(popup, target, args.User);
            }
        }

        // Restores missing blood
        if (healing.ModifyBloodLevel != 0 && bloodstream != null)
            _bloodstreamSystem.TryModifyBloodLevel((target.Owner, bloodstream), healing.ModifyBloodLevel);

        // BEGIN SSU EDITS - Far Horizons limb damage, heal the targetted limb first if it has damage
        bool healingDone;
        DamageSpecifier healed = new();
        DamageSpecifier healedLimb = new();

        var healingDoneToLimb = false;
        if (args.TargettedLimb != null)
        {
            healingDoneToLimb = _limbDamage.TryChangeLimbDamage(target.Owner, args.TargettedLimb.Value,
                healing.Damage * _damageable.UniversalTopicalsHealModifier, out healed, out healedLimb,
                true, origin: args.Args.User);
        }

        if (!healingDoneToLimb || healedLimb.Empty)
            healingDone = _damageable.TryChangeDamage(target.Owner,
                healing.Damage * _damageable.UniversalTopicalsHealModifier, out healed, true,
                origin: args.Args.User);
        else
            healingDone = healingDoneToLimb;

        if (!healingDone && healing.BloodlossModifier != 0)
            return;
        // if (!_damageable.TryChangeDamage(target.Owner, healing.Damage * _damageable.UniversalTopicalsHealModifier, out var healed, true, origin: args.Args.User) && healing.BloodlossModifier != 0)
        //     return;
        // END SSU EDITS

        var total = healed.GetTotal();

        // Re-verify that we can heal the damage.
        var dontRepeat = false;
        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.ReduceCount((args.Used.Value, stackComp), 1);

            if (_stacks.GetCount((args.Used.Value, stackComp)) <= 0)
                dontRepeat = true;
        }
        else
        {
            QueueDel(args.Used.Value);
        }

        if (target.Owner != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed {ToPrettyString(target.Owner):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPredicted(healing.HealingEndSound, target.Owner, args.User);

        // BEGIN SSU EDITS - move on to the next damaged limb once the targetted one is healed
        args.TargettedLimb = PickLimb((args.Used.Value, healing), target, args.TargettedLimb);
        // END SSU EDITS

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = HasDamage((args.Used.Value, healing), target, args.User) && !dontRepeat; // SSU: Far Horizons limb damage, pass the healer
        args.Handled = true;

        if (!args.Repeat)
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-item-finished-using", ("item", args.Used)), target.Owner, args.User);
            return;
        }

        // Update our self heal delay so it shortens as we heal more damage.
        if (args.User == target.Owner)
            args.Args.Delay = healing.Delay * GetScaledHealingPenalty(target.Owner, healing.SelfHealPenaltyMultiplier);
    }

    // BEGIN SSU EDITS - Far Horizons limb damage, keep the limb while damaged, else move on. Null is the body (torso).
    private ProtoId<OrganCategoryPrototype>? PickLimb(Entity<HealingComponent> healing, Entity<DamageableComponent> target,
        ProtoId<OrganCategoryPrototype>? current)
    {
        if (current != null)
            return _limbDamage.GetDamagedLimb(target.Owner, healing.Comp, current);

        return BodyHasDamage(healing, target) ? null : _limbDamage.GetDamagedLimb(target.Owner, healing.Comp);
    }

    private bool BodyHasDamage(Entity<HealingComponent> healing, Entity<DamageableComponent> target)
    {
        var damageableDict = _damageable.GetAllDamage(target.AsNullable()).DamageDict;
        foreach (var type in healing.Comp.Damage.DamageDict)
        {
            if (damageableDict.TryGetValue(type.Key, out var amount) && amount > 0)
                return true;
        }

        return false;
    }
    // END SSU EDITS

    private bool HasDamage(Entity<HealingComponent> healing, Entity<DamageableComponent> target, EntityUid? origin = null) // SSU: origin for limb damage
    {
        // BEGIN SSU EDITS - Far Horizons limb damage, damage on any limb counts too
        if (_limbDamage.GetDamagedLimb(target.Owner, healing.Comp) != null)
            return true;
        // if (origin != null)
        // {
        //     var limbTarget = _limbDamage.GetCurrentValidTarget(target.Owner, origin.Value);
        //     if (limbTarget != null && _limbDamage.LimbHasDamage(target.Owner, limbTarget.Value, healing.Comp))
        //         return true;
        // }
        // END SSU EDITS

        var damageableDict = _damageable.GetAllDamage(target.AsNullable()).DamageDict;
        var healingDict = healing.Comp.Damage.DamageDict;
        foreach (var type in healingDict)
        {
            if (damageableDict.TryGetValue(type.Key, out var amount) && amount > 0)
            {
                return true;
            }
        }

        if (TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            // Is ent missing blood that we can restore?
            if (healing.Comp.ModifyBloodLevel > 0
                && _solutionContainerSystem.ResolveSolution(target.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                && _bloodstreamSystem.GetBloodLevel((target, bloodstream)) < 1)
            {
                return true;
            }

            // Is ent bleeding and can we stop it?
            if (healing.Comp.BloodlossModifier < 0 && bloodstream.BleedAmount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void OnHealingUse(Entity<HealingComponent> healing, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(healing, args.User, args.User))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(Entity<HealingComponent> healing, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(healing, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryHeal(Entity<HealingComponent> healing, Entity<DamageableComponent?> target, EntityUid user)
    {
        if (!Resolve(target, ref target.Comp, false))
            return false;

        if (!TryComp<InjurableComponent>(target, out var injurable))
            return false;

        if (healing.Comp.DamageContainers is not null &&
            injurable.DamageContainer is not null &&
            !healing.Comp.DamageContainers.Contains(injurable.DamageContainer.Value))
        {
            return false;
        }

        if (user != target.Owner && !_interactionSystem.InRangeUnobstructed(user, target.Owner, popup: true))
            return false;

        if (TryComp<StackComponent>(healing, out var stack) && stack.Count < 1)
            return false;

        if (!HasDamage(healing, target!, user)) // SSU: Far Horizons limb damage, pass the healer
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-item-cant-use", ("item", healing.Owner)), healing, user);
            return false;
        }

        _audio.PlayPredicted(healing.Comp.HealingBeginSound, healing, user);

        var isNotSelf = user != target.Owner;

        if (isNotSelf)
        {
            var msg = Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", healing.Owner));
            _popupSystem.PopupEntity(msg, target, target, PopupType.Medium);
        }

        var delay = isNotSelf
            ? healing.Comp.Delay
            : healing.Comp.Delay * GetScaledHealingPenalty(target, healing.Comp.SelfHealPenaltyMultiplier);

        var limbTarget = _limbDamage.GetCurrentValidTarget(target.Owner, user); // SSU: Far Horizons limb damage
        limbTarget = PickLimb(healing, target!, limbTarget); // SSU: start somewhere damaged if the targetted limb is already healed

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, delay, new HealingDoAfterEvent(limbTarget), target, target: target, used: healing) // SSU: pass limbTarget
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    /// <summary>
    /// Scales the self-heal penalty based on the amount of damage taken
    /// </summary>
    /// <param name="ent">Entity we're healing</param>
    /// <param name="mod">Maximum modifier we can have.</param>
    /// <returns>Modifier we multiply our healing time by</returns>
    public float GetScaledHealingPenalty(Entity<DamageableComponent?, MobThresholdsComponent?> ent, float mod)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return mod;

        if (!_mobThresholdSystem.TryGetThresholdForState(ent, MobState.Critical, out var amount, ent.Comp2))
            return 1;

        var percentDamage = (float)(_damageable.GetTotalDamage(ent) / amount);
        //basically make it scale from 1 to the multiplier.

        var output = percentDamage * (mod - 1) + 1;
        return Math.Max(output, 1);
    }
}
