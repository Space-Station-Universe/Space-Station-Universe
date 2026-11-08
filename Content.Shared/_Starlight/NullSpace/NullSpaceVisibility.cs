namespace Content.Shared._Starlight.NullSpace;

/// <summary>
/// SSU: Starlight added NullSpace = 1 << 5 to the upstream VisibilityFlags enum.
/// Kept here as a constant instead so VisibilityFlags stays unedited. Bit 5 is unused upstream.
/// </summary>
public static class NullSpaceVisibility
{
    public const int Layer = 1 << 5;
}
