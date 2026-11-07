using Robust.Client.Graphics;
using Robust.Shared.Player;
using Content.Shared._Starlight.NullSpace;
using Robust.Shared.Prototypes;
using Content.Client._Starlight.Overlay;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight;

public sealed partial class NullSpaceSystem : SharedNullSpaceSystem
{
    [Dependency] private IOverlayManager _overlayMan = default!; // SSU: dropped readonly
    [Dependency] private ISharedPlayerManager _playerMan = default!; // SSU: dropped readonly
    [Dependency] private IPrototypeManager _prototypeManager = default!; // SSU: dropped readonly

    private NullSpaceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NullSpaceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NullSpaceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NullSpaceComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NullSpaceComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ShowNullSpaceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShowNullSpaceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShowNullSpaceComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShowNullSpaceComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ShowNullSpaceComponent, GotEquippedEvent>(GotEquippedEvent);

        _overlay = new(_prototypeManager.Index(NullSpaceShader)); // SSU: analyzer forbids literal prototype IDs
    }

    private static readonly ProtoId<ShaderPrototype> NullSpaceShader = "NullSpaceShader"; // SSU: analyzer forbids literal prototype IDs

    private void OnInit(EntityUid uid, Component component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        if (component.GetType() == typeof(ShowNullSpaceComponent))
        {
            ShowNullSpaceComponent showNullSpace = (ShowNullSpaceComponent)component;
            if (!showNullSpace.ShowShader)
                return;
        }

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, Component component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        if (component.GetType() == typeof(ShowNullSpaceComponent) && HasComp<NullSpaceComponent>(uid))
            return;

        if (component.GetType() == typeof(NullSpaceComponent) && HasComp<ShowNullSpaceComponent>(uid))
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void GotEquippedEvent(EntityUid uid, ShowNullSpaceComponent component, GotEquippedEvent args)
    {
        if (args.EquipTarget != _playerMan.LocalEntity // SSU: upstream renamed Equipee to EquipTarget
            || !component.ShowShader)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, Component component, LocalPlayerAttachedEvent args)
    {
        if (component.GetType() == typeof(ShowNullSpaceComponent))
        {
            ShowNullSpaceComponent showNullSpace = (ShowNullSpaceComponent)component;
            if (!showNullSpace.ShowShader)
                return;
        }

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, Component component, LocalPlayerDetachedEvent args)
    {
        if (component.GetType() == typeof(ShowNullSpaceComponent) && HasComp<NullSpaceComponent>(uid))
            return;

        if (component.GetType() == typeof(NullSpaceComponent) && HasComp<ShowNullSpaceComponent>(uid))
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }
}