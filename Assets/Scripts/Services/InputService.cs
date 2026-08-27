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

        foreach (CinemachineCamera camera in _cameras)
            Debug.Log($"CAMERA FOUND!!! {camera.name}");

        base.Initialize();
    }

    protected override void OnInitialize()
    {
        PlayerActions.Enable();
        UIActions.Enable();

        DisablePlayerControls();

        Debug.Log($"{GetType().Name} initialized with the following dependencies: Input Actions");
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
    }

    public void DisablePlayerControls()
    {
        SetCursorState(false);
        ToggleCameraInput(false);
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

    private void OnDestroy()
    {
        Dispose();
    }
}
