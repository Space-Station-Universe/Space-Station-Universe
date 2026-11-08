namespace Content.Shared._FarHorizons.Body;

[RegisterComponent]
public sealed partial class OrganWithInventorySlotsComponent : Component
{
    [DataField] public List<string> Slots = new();
}

[RegisterComponent]
public sealed partial class NeedsOrgansForInventorySlotsComponent : Component
{
    [DataField] public List<string> Slots = new();
}