using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class InputService : ServiceBase, IInputService
{
    [Header("References")]
    public InputActions.PlayerActions PlayerActions => _inputActions.Player;
    public InputActions.UIActions UIActions => _inputActions.UI;

    private InputActions _inputActions;
    private List<CinemachineCamera> _cameras;

    public override void Initialize()
    {
        _inputActions = new();
        _cameras = new List<CinemachineCamera>(FindObjectsByType<CinemachineCamera>());

        base.Initialize();
    }

    protected override void OnInitialize()
    {
        PlayerActions.Enable();
        UIActions.Enable();

        DisablePlayerControls();

        Debug.Log($"{GetType().Name} initialized with dependencies: Input Actions");
    }

    public void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    public void EnablePlayerControls()
    {
        SetCursorState(true);
        ToggleCameraInput(true);
        PlayerActions.Enable();
        UIActions.Enable();
        _inputActions.Player.Enable();
        _inputActions.UI.Enable();
    }

    public void DisablePlayerControls()
    {
        SetCursorState(false);
        ToggleCameraInput(false);
        PlayerActions.Disable();
        UIActions.Disable();
        _inputActions.Player.Disable();
        _inputActions.UI.Disable();
    }

    public void SetCameraSensitivity(float sensitivityValue)
    {
        foreach (CinemachineCamera cinemachineCamera in _cameras)
        {
            if (cinemachineCamera == null)
                continue;

            if (!cinemachineCamera.TryGetComponent(out CinemachineInputAxisController cinemachineInputAxisController))
                continue;

            foreach (var controller in cinemachineInputAxisController.Controllers)
            {
                if (controller.Name == "Look X (Pan)")
                    controller.Input.Gain = sensitivityValue;
                else if (controller.Name == "Look Y (Tilt)")
                    controller.Input.Gain = -sensitivityValue;
            }
        }
    }

    private void ToggleCameraInput(bool isEnabled)
    {
        foreach (CinemachineCamera cinemachineCamera in _cameras)
        {
            if (cinemachineCamera == null)
                continue;

            if (cinemachineCamera.TryGetComponent(out CinemachineInputAxisController cinemachineInputAxisController))
                cinemachineInputAxisController.enabled = isEnabled;
        }
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
