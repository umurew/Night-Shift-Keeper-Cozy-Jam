using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Configuration")]
    [SerializeField] private float gravity = -9.81f;

    [Space(10)]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private bool canSprint = false;
    [SerializeField] private bool canCrouch = false;
    [SerializeField] private bool canJump = false;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float crouchSpeed = 1f;
    [SerializeField] private float jumpHeight = 1f;

    [Space(10)]
    [SerializeField] private float stepInterval = 0.45f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float minSpeedThreshold = 0.2f;
    [Range(0f, 1f)][SerializeField] private float volume = 0.5f;

    private IInputService _inputService;
    private SceneBlackboard _sceneBlackboard;
    private Transform _cameraTransform;

    private AudioSource _audioSource;
    private CharacterController _characterController;

    private bool _initialized = false;
    private float _verticalVelocity = 0f;
    private bool _crouching = false;

    private float _stepTimer;
    private int _lastPlayedIndex = -1;

    private Dictionary<string, int> _animationHashes;

    public void Initialize(IInputService inputService, SceneBlackboard sceneBlackboard, Transform cameraTransform)
    {
        _inputService = inputService;
        _sceneBlackboard = sceneBlackboard;
        _cameraTransform = cameraTransform;

        _audioSource = GetComponent<AudioSource>();
        _characterController = GetComponent<CharacterController>();

        _animationHashes = new()
        {
            { "BlendHorizontal", Animator.StringToHash("BlendHorizontal") },
            { "BlendVertical", Animator.StringToHash("BlendVertical") },
            { "Jump", Animator.StringToHash("Jump") },
            { "IsGrounded", Animator.StringToHash("IsGrounded") },
            { "Crouching", Animator.StringToHash("Crouching") }
        };

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.CanSprint, () =>
        {
            canSprint = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.CanSprint);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.CanCrouch, () =>
        {
            canCrouch = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.CanCrouch);
        });

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Player.CanJump, () =>
        {
            canJump = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.CanJump);
        });

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with the following dependencies: {inputService.GetType().Name} | {cameraTransform.GetType().Name} | {sceneBlackboard.GetType().Name}");
    }

    private void Update()
    {
        if (!_initialized)
            return;

        // Handle player rotation with camera
        Vector3 cameraEulerAngles = _cameraTransform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, cameraEulerAngles.y, 0f);

        // Read and calculate movement input
        Vector2 input = _inputService.PlayerActions.Move.ReadValue<Vector2>();

        Vector3 forwardVector = _cameraTransform.forward;
        Vector3 rightVector = _cameraTransform.right;

        forwardVector.y = 0f;
        forwardVector.Normalize();

        rightVector.y = 0f;
        rightVector.Normalize();

        Vector3 horizontalVelocity = forwardVector * input.y + rightVector * input.x;

        // Handle ground check and gravity
        if (_characterController.isGrounded)
        {
            // Reset vertical velocity to prevent infinite falling
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // Handle crouching
            if (canCrouch && _inputService.PlayerActions.Crouch.IsInProgress())
            {
                _crouching = true;

                _characterController.center = new Vector3(0f, 0.5f, 0f);
                _characterController.height = 1f;
            }
            else
            {
                _crouching = false;

                _characterController.center = new Vector3(0f, 0.925f, 0f);
                _characterController.height = 1.85f;
            }

            // Handle jumping
            if (canJump && _inputService.PlayerActions.Jump.WasPressedThisFrame())
            {
                _crouching = false;
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                animator.SetTrigger(_animationHashes["Jump"]);
            }
        }
        else
            _verticalVelocity += gravity * Time.deltaTime;

        // Handle sprinting
        bool sprinting = canSprint && _inputService.PlayerActions.Sprint.IsInProgress();

        float horizontalSpeed = true switch
        {
            _ when sprinting => sprintSpeed,
            _ when _crouching => crouchSpeed,
            _ => walkSpeed
        };

        Vector3 compositeVelocity = horizontalSpeed * horizontalVelocity + Vector3.up * _verticalVelocity;
        _characterController.Move(compositeVelocity * Time.deltaTime);

        UpdateParameters(input, sprinting, _crouching);
        HandleFootsteps(sprinting, _crouching);
    }

    private void HandleFootsteps(bool sprinting, bool crouching)
    {
        Vector3 horizontalVelocity = new(_characterController.velocity.x, 0, _characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (!_characterController.isGrounded || currentSpeed < 0.1f)
        {
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.NoiseScore, 0f);
            return;
        }

        _sceneBlackboard.Set(SceneBlackboardKeys.Player.NoiseScore, currentSpeed);

        if (_characterController.isGrounded && currentSpeed > minSpeedThreshold)
        {
            float dynamicInterval = stepInterval * (walkSpeed / Mathf.Max(currentSpeed, 0.1f));

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= dynamicInterval)
            {
                PlayRandomFootstep(sprinting, crouching);
                _stepTimer = 0f;
            }
        }
        else
            _stepTimer = stepInterval;
    }

    private void PlayRandomFootstep(bool sprinting, bool crouching)
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return;

        int randomIndex;

        if (footstepClips.Length > 1)
        {
            do
            {
                randomIndex = Random.Range(0, footstepClips.Length);
            }
            while (randomIndex == _lastPlayedIndex);
        }
        else
            randomIndex = 0;

        float finalVolume = true switch
        {
            _ when sprinting => volume * 1.1f,
            _ when crouching => volume * 0.4f,
            _ => volume
        };

        _lastPlayedIndex = randomIndex;

        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        _audioSource.volume = finalVolume;
        _audioSource.PlayOneShot(footstepClips[randomIndex]);
    }

    private void UpdateParameters(Vector2 input, bool sprinting, bool crouching)
    {
        float modifier = sprinting ? 2f : 1f;
        animator.SetFloat(_animationHashes["BlendHorizontal"], input.x * modifier, 0.1f, Time.deltaTime);
        animator.SetFloat(_animationHashes["BlendVertical"], input.y * modifier, 0.1f, Time.deltaTime);

        animator.SetBool(_animationHashes["IsGrounded"], _characterController.isGrounded);
        animator.SetBool(_animationHashes["Crouching"], crouching);

        int crouchLayerIndex = animator.GetLayerIndex("Crouch Layer");
        if (crouchLayerIndex == -1)
        {
            Debug.LogWarning($"Crouch layer was not found.");
            return;
        }

        float targetWeight = crouching ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(crouchLayerIndex);

        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 5f);
        animator.SetLayerWeight(crouchLayerIndex, newWeight);
    }
}
