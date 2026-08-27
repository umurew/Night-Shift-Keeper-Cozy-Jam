using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Configuration")]
    [SerializeField] private float raycastDistance = 3.5f;
    [SerializeField] private float raycastRadius = 0.02f;
    [SerializeField] private LayerMask raycastLayerMask;
    [SerializeField] private string containerName = "PromptContainer";
    [SerializeField] private string headerName = "PromptHeader";
    [SerializeField] private string labelName = "PromptLabel";

    private IInputService _inputService;
    private Transform _cameraTransform;
    private bool _initialized = false;
    private IInteractable _currentInteractable;
    private RaycastHit _currentHit;
    private VisualElement _promptContainer;
    private Label _promptHeaderLabel;
    private Label _promptLabel;

    public void Initialize(IInputService inputService, Transform cameraTransform)
    {
        _inputService = inputService;
        _cameraTransform = cameraTransform;

        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogWarning($"UIDocument is missing or not initialized on {GetType().Name}.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        _promptContainer = root.Q<VisualElement>(containerName);
        _promptHeaderLabel = root.Q<Label>(headerName);
        _promptLabel = root.Q<Label>(labelName);

        if (_promptContainer == null || _promptHeaderLabel == null || _promptLabel == null)
        {
            Debug.LogWarning($"{GetType().Name} failed to initialize: UI elements were missing.");
            return;
        }

        HidePrompt();

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with the following dependencies: Input Service | Camera Transform");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        PerformInteractionCheck();

        if (_currentInteractable != null && _inputService.PlayerActions.Interact.WasPressedThisFrame())
        {
            _currentInteractable.Interact();
            UpdatePrompt();
        }
    }

    private void PerformInteractionCheck()
    {
        Ray ray = new(_cameraTransform.position, _cameraTransform.forward);

        if (Physics.SphereCast(ray, raycastRadius, out RaycastHit raycastHit, raycastDistance, raycastLayerMask))
        {
            _currentHit = raycastHit;
            IInteractable interactable = raycastHit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.Interactable == true)
            {
                if (interactable == _currentInteractable && interactable.GetInteractPrompt() == _promptLabel.text)
                    return;

                _currentInteractable = interactable;
                ShowPrompt();

                return;
            }
        }

        if (_currentInteractable != null)
        {
            _currentInteractable = null;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        if (_currentInteractable == null || _promptContainer == null || _promptHeaderLabel == null || _promptLabel == null)
        {
            Debug.LogError("One or more of the visual elements or variable \"_currentInteractable\" was null.");
            return;
        }

        UpdatePrompt();
        _promptContainer.style.visibility = Visibility.Visible;
    }

    private void HidePrompt()
    {
        if (_promptContainer != null)
            _promptContainer.style.visibility = Visibility.Hidden;
    }

    private void UpdatePrompt()
    {
        if (_currentInteractable == null)
            return;

        _promptLabel.text = _currentInteractable.GetInteractPrompt();
        _promptHeaderLabel.text = $"{_inputService.PlayerActions.Interact.GetBindingDisplayString(0)} to Interact";
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
