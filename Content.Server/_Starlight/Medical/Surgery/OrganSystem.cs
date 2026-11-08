using System.Linq;
using Content.Server._Starlight.Language;
using Content.Shared._Starlight.Language.Components.Translators;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Radio.Components;
// using Content.Shared.Speech.Muting; // SSU: upstream replaced MutedComponent with a status effect
// using Content.Shared.Starlight.Antags.Abductor; // SSU: abductors not ported
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
// using Content.Shared.VentCraw; // SSU: vent crawl not ported
using Content.Shared.StatusEffectNew; // SSU: muted status effect
using Robust.Shared.Prototypes; // ditto
using Robust.Shared.Containers;
using Robust.Shared.Timing;
// using Content.Shared.Starlight; // SSU: Starlight cyber limbs not ported

namespace Content.Server._Starlight.Medical.Surgery;
public sealed partial class OrganSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!; // SSU: muted status effect

    private static readonly EntProtoId MutedEffect = "StatusEffectMuted"; // SSU
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotInsertedEvent>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, OrganGotRemovedEvent>(OnFunctionalOrganExtracted);

        // SSU: Starlight cyber limbs not ported
        // SubscribeLocalEvent<LimbWithActionComponent, OrganGotInsertedEvent>(OnLimbwithActionOrganImplanted);
        // SubscribeLocalEvent<LimbWithActionComponent, OrganGotRemovedEvent>(OnLimbwithActionOrganExtracted);

        SubscribeLocalEvent<StorageOrganComponent, OrganGotInsertedEvent>(OnStorageOrganImplanted);
        SubscribeLocalEvent<StorageOrganComponent, OrganGotRemovedEvent>(OnStorageOrganExtracted);

        SubscribeLocalEvent<OrganTongueComponent, OrganGotInsertedEvent>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, OrganGotRemovedEvent>(OnTongueExtracted);

        // SSU: abductors not ported
        // SubscribeLocalEvent<AbductorOrganComponent, OrganGotInsertedEvent>(OnAbductorOrganImplanted);
        // SubscribeLocalEvent<AbductorOrganComponent, OrganGotRemovedEvent>(OnAbductorOrganExtracted);

        SubscribeLocalEvent<OrganDamageComponent, OrganGotInsertedEvent>(OnOrganImplanted);
        SubscribeLocalEvent<OrganDamageComponent, OrganGotRemovedEvent>(OnOrganExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
        {
            if (!HasComp(args.Target, comp.Component.GetType()))
            {
                AddComp(args.Target, comp.Component);
                UpdateEntity(args.Target, comp.Component, ent.Owner);
            }
        }
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
        {
            if (HasComp(args.Target, comp.Component.GetType()))
            {
                RemComp(args.Target, EntityManager.GetComponent(args.Target, comp.Component.GetType()));
                UpdateEntity(args.Target, comp.Component, ent.Owner);
            }
        }
    }

    private void UpdateEntity(EntityUid ent, IComponent comp, EntityUid? implant = null)
    {
        //For all those components where the enity needs to be updated in their own way after adding or removing a component
        switch (comp)
        {
            case IntrinsicTranslatorComponent _:
                _language.UpdateEntityLanguages(ent);
                break;
            case EncryptionKeyHolderComponent encrypt: //Move encryption keys between implant and body
                if(implant != null)
                    if(TryComp(implant, out EncryptionKeyHolderComponent? implantKeyHolder))
                        if (TryComp(ent, out EncryptionKeyHolderComponent? bodyKeyHolder))
                            foreach (var key in implantKeyHolder.KeyContainer.ContainedEntities.ToList())
                                _container.Insert(key, bodyKeyHolder.KeyContainer);
                        else
                            foreach (var key in encrypt.KeyContainer.ContainedEntities.ToList())
                                _container.Insert(key, implantKeyHolder.KeyContainer);
                break;
        }
    }

    // SSU: Starlight cyber limbs not ported
    // private void OnLimbwithActionOrganImplanted(Entity<LimbWithActionComponent> ent, ref OrganGotInsertedEvent args)
    // {
    //     var actionEntity = ent.Comp.ActionEntity;
    //     _action.AddAction(args.Target, ref actionEntity, ent.Comp.Action, ent.Owner);
    //     ent.Comp.ActionEntity = actionEntity;
    // }
    //
    // private void OnLimbwithActionOrganExtracted(Entity<LimbWithActionComponent> ent, ref OrganGotRemovedEvent args)
    //     => _action.RemoveAction(args.Target, ent.Comp.ActionEntity);

    private void OnStorageOrganImplanted(Entity<StorageOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        Dirty(ent);

        if (ent.Comp.OrganAction != null)
            _action.AddAction(args.Target, ref ent.Comp.ActionEntity, ent.Comp.OrganAction, ent.Owner);

        var limbComp = EnsureComp<PrivateStorageLimbComponent>(args.Target);
        limbComp.Limb.Add(ent.Owner);
    }

    private void OnStorageOrganExtracted(Entity<StorageOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        _action.RemoveAction(args.Target, ent.Comp.ActionEntity);
        ent.Comp.ActionEntity = null;

        if(TryComp<PrivateStorageLimbComponent>(args.Target, out var slComp))
        {
            slComp.Limb.Remove(ent.Owner);
            if(slComp.Limb.Count == 0)
                RemCompDeferred<PrivateStorageLimbComponent>(args.Target);
        }
    }

    private void OnOrganImplanted(Entity<OrganDamageComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!TryComp<DamageableComponent>(args.Target, out var bodyDamageable)
            || ent.Comp.StoredDamage == null) return;

        _damageableSystem.ChangeDamage((args.Target, bodyDamageable), -ent.Comp.StoredDamage, true, false); // SSU: Far Horizons Invert helper, upstream has unary minus
        ent.Comp.StoredDamage = null;
    }
    private void OnOrganExtracted(Entity<OrganDamageComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        if (ent.Comp.Damage is null
         || !TryComp<DamageableComponent>(args.Target, out var bodyDamageable)) return;

        ent.Comp.StoredDamage = _damageableSystem.ChangeDamage((args.Target, bodyDamageable), -ent.Comp.Damage, true, false); // SSU: Far Horizons Invert helper, upstream has unary minus
    }

    // SSU: abductors and vent crawl not ported
    // private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref OrganGotInsertedEvent args)
    // {
    //     if (TryComp<AbductorVictimComponent>(args.Target, out var victim))
    //         victim.Organ = ent.Comp.Organ;
    //     if (ent.Comp.Organ == AbductorOrganType.Vent)
    //         AddComp<VentCrawlerComponent>(args.Target);
    // }
    // private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref OrganGotRemovedEvent args)
    // {
    //     if (TerminatingOrDeleted(ent)) return;
    //     if (TryComp<AbductorVictimComponent>(args.Target, out var victim))
    //         if (victim.Organ == ent.Comp.Organ)
    //             victim.Organ = AbductorOrganType.None;
    //
    //     if (ent.Comp.Organ == AbductorOrganType.Vent)
    //         RemComp<VentCrawlerComponent>(args.Target);
    // }

    //

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref OrganGotInsertedEvent args)
    {
        // BEGIN SSU EDITS - abductors not ported, upstream mute is a status effect
        if (ent.Comp.IsMuted) return;

        _statusEffects.TryRemoveStatusEffect(args.Target, MutedEffect);
        // if (HasComp<AbductorComponent>(args.Target) || ent.Comp.IsMuted) return;
        //
        // if (HasComp<MutedComponent>(args.Target))
        //     RemComp<MutedComponent>(args.Target);
        // END SSU EDITS
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent)) return;
        // BEGIN SSU EDITS - upstream mute is a status effect
        ent.Comp.IsMuted = _statusEffects.HasStatusEffect(args.Target, MutedEffect);
        _statusEffects.TrySetStatusEffectDuration(args.Target, MutedEffect);
        // ent.Comp.IsMuted = HasComp<MutedComponent>(args.Target);
        // EnsureComp<MutedComponent>(args.Target);
        // END SSU EDITS
    }
}
