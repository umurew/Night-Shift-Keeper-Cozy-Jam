using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument promptDocument;

    [Header("Configuration")]
    [SerializeField] private bool canInteract = false;

    [Space(10)]
    [SerializeField] private float raycastDistance = 3.5f;
    [SerializeField] private float raycastRadius = 0.02f;
    [SerializeField] private LayerMask raycastLayerMask;

    private IInputService _inputService;
    private SceneBlackboard _sceneBlackboard;
    private Transform _cameraTransform;

    private bool _initialized = false;

    private IInteractable _currentInteractable;
    private RaycastHit _currentHit;

    private VisualElement _promptContainer;
    private Label _promptHeaderLabel;
    private Label _promptSubLabel;

    public void Initialize(IInputService inputService, SceneBlackboard sceneBlackboard, Transform cameraTransform)
    {
        _inputService = inputService;
        _sceneBlackboard = sceneBlackboard;
        _cameraTransform = cameraTransform;

        VisualElement rootVisualElement = promptDocument.rootVisualElement;
        _promptContainer = rootVisualElement.Q<VisualElement>("prompt-container");
        _promptHeaderLabel = rootVisualElement.Q<Label>("prompt-header-label");
        _promptSubLabel = rootVisualElement.Q<Label>("prompt-sub-label");

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.CanInteract, OnCanInteractChanged);

        HideInterface();

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with dependencies: {inputService.GetType().Name} | {cameraTransform.GetType().Name} | {sceneBlackboard.GetType().Name}");
    }

    private void OnCanInteractChanged()
    {
        canInteract = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.CanInteract);

        if (!canInteract)
            ClearInteraction();
    }

    private void Update()
    {
        if (!_initialized || !canInteract)
            return;

        PerformInteractionCheck();

        if (_currentInteractable != null && _inputService.PlayerActions.Interact.WasPressedThisFrame())
        {
            _currentInteractable.Interact();
            ShowInterface();
        }
    }

    private void PerformInteractionCheck()
    {
        Ray ray = new(_cameraTransform.position, _cameraTransform.forward);

        if (Physics.SphereCast(ray, raycastRadius, out RaycastHit raycastHit, raycastDistance, raycastLayerMask))
        {
            _currentHit = raycastHit;
            IInteractable interactable = raycastHit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.Interactable)
            {
                if (interactable == _currentInteractable && interactable.GetInteractPrompt() == _promptHeaderLabel.text)
                    return;

                _currentInteractable = interactable;
                ShowInterface();
                return;
            }
        }

        ClearInteraction();
    }

    private void ClearInteraction()
    {
        if (_currentInteractable != null || _promptContainer.style.visibility == Visibility.Visible)
        {
            _currentInteractable = null;
            HideInterface();
        }
    }

    private void HideInterface() => _promptContainer.style.visibility = Visibility.Hidden;

    private void ShowInterface()
    {
        if (_currentInteractable == null)
            return;

        _promptContainer.style.visibility = Visibility.Visible;
        _promptHeaderLabel.text = _currentInteractable.GetInteractPrompt();

        string bindingDisplay = _inputService.PlayerActions.Interact.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"))
            .Replace("Hold ", "")
            .Trim();

        _promptSubLabel.text = $"Press {bindingDisplay} to Interact";
    }

    private void OnDrawGizmosSelected()
    {
        if (_cameraTransform == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * raycastDistance);

        float displayDistance = _currentInteractable != null ? _currentHit.distance : raycastDistance;
        Gizmos.DrawWireSphere(_cameraTransform.position + (_cameraTransform.forward * displayDistance), raycastRadius);
    }
}