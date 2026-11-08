using Robust.Shared.Configuration;

namespace Content.Shared._FarHorizons.CCVar;

// Ported from Far Horizons Content.Shared/_FarHorizons/CCVar/FHCCVars.cs, only the limb targetting CVars for now.
[CVarDefs]
public sealed partial class FHCCVars
{
    public static readonly CVarDef<string> LimbTargettingStyle =
        CVarDef.Create("ui.limb_targetting_style", "LimbTargetHuman",
            CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> LimbTargettingMatchSpecies =
        CVarDef.Create("ui.limb_targetting_match_species", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
