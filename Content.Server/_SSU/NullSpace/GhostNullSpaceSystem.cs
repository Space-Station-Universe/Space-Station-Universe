using Content.Shared._Starlight.NullSpace;
using Content.Shared.Ghost.Components;

namespace Content.Server._SSU.NullSpace;

/// <summary>
/// Lets ghosts see entities in NullSpace.
/// Starlight adds ShowNullSpace to the upstream observer.yml, this does it without editing upstream YAML.
/// </summary>
public sealed partial class GhostNullSpaceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // GhostSystem already uses ComponentStartup and MapInit, Init runs before both so the ghost's first eye refresh includes it.
        SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostInit);
    }

    private void OnGhostInit(Entity<GhostComponent> ent, ref ComponentInit args)
    {
        EnsureComp<ShowNullSpaceComponent>(ent);
    }
}
