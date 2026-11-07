using JetBrains.Annotations;
using Content.Client._Starlight.Language;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.UserInterface.Systems.Gameplay; // SSU
using Content.Shared._SSU.Input;                     // SSU
using Robust.Client.Input;                           // SSU

namespace Content.Client._Starlight.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed partial class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState> // SSU: added partial cuz of robust update
{
    [Dependency] private IInputManager _input = default!; // SSU: register the key function without editing ContentContexts

    public LanguageMenuWindow? LanguageWindow;
    private MenuButton? LanguageButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.LanguageButton;

    // BEGIN SSU EDITS - load the menu button without editing GameTopMenuBarUIController
    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButton;
        gameplayStateLoad.OnScreenUnload += UnloadButton;
    }
    // END SSU EDITS

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(LanguageWindow == null);

        // BEGIN SSU EDITS - register the key function without editing ContentContexts
        var human = _input.Contexts.GetContext("human");
        if (!human.FunctionExists(SSUKeyFunctions.OpenLanguageMenu))
            human.AddFunction(SSUKeyFunctions.OpenLanguageMenu);
        // END SSU EDITS

        LanguageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(LanguageWindow, LayoutContainer.LayoutPreset.CenterTop);

        LanguageWindow.OnClose += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = false;
        };
        LanguageWindow.OnOpen += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = true;
        };

        CommandBinds.Builder.Bind(SSUKeyFunctions.OpenLanguageMenu, // SSU: key function lives in _SSU instead of ContentKeyFunctions
            InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (LanguageWindow != null)
        {
            LanguageWindow.Dispose();
            LanguageWindow = null;
        }

        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    public void UnloadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed += LanguageButtonPressed;
    }

    private void LanguageButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (LanguageWindow == null)
            return;

        if (LanguageButton != null)
            LanguageButton.SetClickPressed(!LanguageWindow.IsOpen);

        if (LanguageWindow.IsOpen)
            LanguageWindow.Close();
        else
            LanguageWindow.Open();
    }
}