using System.Diagnostics.CodeAnalysis;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.UserInterface.RichText; // SSU: moved into _Starlight, upstream removed IconTag

// Ported from Starlight Content.Client/UserInterface/RichText/IconTag.cs, used for the language icon in chat.
public sealed partial class IconTag : IMarkupTagHandler // SSU: robust renamed IMarkupTag to IMarkupTagHandler, added partial
{
    [Dependency] private IPrototypeManager _prototype = default!;     // SSU: removed readonly
    [Dependency] private IEntitySystemManager _entitySystem = default!; // ditto
    private SpriteSystem? _spriteSystem;
    // [Dependency] private readonly Robust.Shared.Configuration.IConfigurationManager _cfg = default!; // SSU: DisableJobIconAnimation CVar is Starlight-only

    public string Name => "icon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control) // SSU: robust renamed TryGetControl to TryCreateControl
    {
        if (!node.Attributes.TryGetValue("src", out var id) || id.StringValue == null)
        {
            control = null;
            return false;
        }
        _spriteSystem ??= _entitySystem.GetEntitySystem<SpriteSystem>();
        /* Starlight start */
        AnimatedTextureRect? animated = null;
        TextureRect? icon = null;

        _prototype.TryIndex<JobIconPrototype>(id.StringValue, out var jobProto);

        if (jobProto != null)
        {
            var spec = jobProto.Icon;
            try
            {
                var state = _spriteSystem.RsiStateLike(spec);
                // var disableJobAnim = _cfg.GetCVar(CCVars.DisableJobIconAnimation); // SSU: Starlight-only CVar

                if (state.IsAnimated) // SSU: was state.IsAnimated && !disableJobAnim
                {
                    var anim = new AnimatedTextureRect();
                    anim.SetFromSpriteSpecifier(spec);
                    anim.DisplayRect.SetWidth = 20;
                    anim.DisplayRect.SetHeight = 20;
                    anim.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
                    anim.MouseFilter = Control.MouseFilterMode.Stop;
                    animated = anim;
                }
                else
                {
                    var texture = _spriteSystem.Frame0(spec);
                    icon = new TextureRect
                    {
                        Texture = texture,
                        SetWidth = 20,
                        SetHeight = 20,
                        Stretch = TextureRect.StretchMode.Scale,
                        MouseFilter = Control.MouseFilterMode.Stop,
                    };
                }
            }
            catch
            {
                // If anything goes wrong, fall back to texture
                var texture = _spriteSystem.Frame0(spec);
                icon = new TextureRect
                {
                    Texture = texture,
                    SetWidth = 20,
                    SetHeight = 20,
                    Stretch = TextureRect.StretchMode.Scale,
                    MouseFilter = Control.MouseFilterMode.Stop,
                };
            }
        }
        if (node.Attributes.TryGetValue("tooltip", out var tooltip) && tooltip.StringValue != null)
        {
            if (animated != null)
                animated.ToolTip = tooltip.StringValue;
            else if (icon != null)
                icon.ToolTip = tooltip.StringValue;
        }

        control = (Control?)animated ?? icon;
        return control != null;
        // Starlight end
    }
}
