using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerFlashlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject flashlight;
    [SerializeField] private ParentConstraint parentConstraint;

    [Space(10)]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private AudioClip flashlightClick;

    private IInputService _inputService;
    private AudioSource _audioSource;
    private Dictionary<string, int> _animationHashes;
    private bool _initialized = false;
    private bool _flashlightEquipped = false;
    private bool _debounce = false;
    private static readonly WaitForSeconds cooldown = new(0.25f);
    private static readonly WaitForSeconds actionDelay = new(0.25f);

    public void Initialize(IInputService inputService)
    {
        _inputService = inputService;
        _audioSource = flashlight.GetComponent<AudioSource>();

        _initialized = true;

        Debug.Log($"{GetType().Name} initialized with the following dependencies: Input Service | Sound Service");
    }

    private void Awake()
    {
        _animationHashes = new()
        {
            { "FlashlightEquipped", Animator.StringToHash("FlashlightEquipped") },
        };
    }

    private void Update()
    {
        if (!_initialized)
            return;

        if (!_debounce && _inputService.PlayerActions.ToggleFlashlight.WasPressedThisFrame())
        {
            _debounce = true;

            _flashlightEquipped = !_flashlightEquipped;

            if (_flashlightEquipped)
                StartCoroutine(EquipFlashlight());
            else
                StartCoroutine(UnequipFlashlight());

            animator.SetBool(_animationHashes["FlashlightEquipped"], _flashlightEquipped);
        }
        
        UpdateLayerWeight();
    }

    private void UpdateLayerWeight()
    {
        int viewmodelLayerIndex = animator.GetLayerIndex("Viewmodel Layer");

        if (viewmodelLayerIndex == -1)
        {
            Debug.LogWarning($"Viewmodel layer was not found.");
            return;
        }

        float targetWeight = _flashlightEquipped ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(viewmodelLayerIndex);

        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 5f);
        animator.SetLayerWeight(viewmodelLayerIndex, newWeight);
    }

    private void SetSourceWeight(int sourceIndex, float weight)
    {
        if (parentConstraint == null || sourceIndex < 0 || sourceIndex >= parentConstraint.sourceCount)
        {
            Debug.LogWarning($"Invalid constraint or source index.");
            return;
        }

        ConstraintSource source = parentConstraint.GetSource(sourceIndex);
        source.weight = Mathf.Clamp01(weight);

        parentConstraint.SetSource(sourceIndex, source);
    }

    private IEnumerator UnequipFlashlight()
    {
        _audioSource.PlayOneShot(flashlightClick);
        flashlightLight.enabled = false;

        yield return actionDelay;

        SetSourceWeight(0, 0);
        SetSourceWeight(1, 1);

        yield return cooldown;
        _debounce = false;
    }

    private IEnumerator EquipFlashlight()
    {
        SetSourceWeight(0, 1);
        SetSourceWeight(1, 0);

        _audioSource.PlayOneShot(flashlightClick);
        flashlightLight.enabled = true;

        yield return cooldown;
        _debounce = false;
    }
}
