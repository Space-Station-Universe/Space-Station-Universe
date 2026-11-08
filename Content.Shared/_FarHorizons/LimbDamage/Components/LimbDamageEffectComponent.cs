using Content.Shared._FarHorizons.LimbDamage.Effect;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.LimbDamage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbDamageEffectComponent: Component
{
    [DataField] public List<LimbDamageEffect> Effects;

    public DamageableLimbComponent? DamageableLimb;
}