using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared._Starlight.NullSpace;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._Starlight.NullSpace;

public sealed partial class ShowNullSpaceSystem : SharedShowNullSpaceSystem
{
    [Dependency] private EyeSystem _eye = default!; // SSU: dropped readonly
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShowNullSpaceComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ShowNullSpaceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShowNullSpaceComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ShowNullSpaceComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ShowNullSpaceComponent, GetVisMaskEvent>(OnGetVisMask); // SSU: upstream eyes build their mask from GetVisMaskEvent
    }

    private void OnInit(EntityUid uid, ShowNullSpaceComponent component, MapInitEvent args)
    {
        Toggle(uid, true);
    }

    public void OnShutdown(EntityUid uid, ShowNullSpaceComponent component, ComponentShutdown args)
    {
        Toggle(uid, false);
    }

    private void OnEquipped(EntityUid uid, ShowNullSpaceComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        CopyComp(uid, args.EquipTarget, component); // SSU: analyzer wants the CopyComp proxy, upstream renamed Equipee to EquipTarget
    }

    private void OnUnequipped(EntityUid uid, ShowNullSpaceComponent component, GotUnequippedEvent args)
    {
        RemComp<ShowNullSpaceComponent>(args.EquipTarget); // SSU: upstream renamed Equipee to EquipTarget
    }

    // SSU: rewritten for upstream GetVisMaskEvent. Starlight set the mask directly, which upstream refreshes overwrite, and turning it off set the mask to Normal, which also wiped other layers (like a ghost's).
    private void Toggle(EntityUid uid, bool toggle)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        _eye.RefreshVisibilityMask(uid);
    }

    private void OnGetVisMask(Entity<ShowNullSpaceComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.VisibilityMask |= NullSpaceVisibility.Layer;
    }
}
