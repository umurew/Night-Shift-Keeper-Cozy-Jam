using UnityEngine;

public class InputService : ServiceBase, IInputService
{
    private InputActions _inputActions;

    public InputActions.PlayerActions PlayerActions => _inputActions.Player;
    public InputActions.UIActions UIActions => _inputActions.UI;

    public override void Initialize()
    {
        _inputActions = new();
        base.Initialize();
    }

    protected override void OnInitialize()
    {
        PlayerActions.Enable();
        UIActions.Enable();

        Debug.Log($"{GetType().Name} initialized with the following dependencies:\n\t- InputActions");
    }

    public void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    public void EnablePlayerControls()
    {
        SetCursorState(true);
        PlayerActions.Enable();
    }

    public void DisablePlayerControls()
    {
        SetCursorState(false);
        PlayerActions.Disable();
    }

    public void Dispose()
    {
        if (_inputActions != null)
        {
            PlayerActions.Disable();
            UIActions.Disable();
            _inputActions.Dispose();
            _inputActions = null;
        }

        Debug.Log($"{GetType().Name} disposed.");
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
