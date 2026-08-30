using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Deer : MonoBehaviour, IInteractable
{
    private enum DeerState { Wandering, Fleeing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;

    [Space(10)]
    [SerializeField] private float gravity = -9.81f;

    [Space(10)]
    [SerializeField] private bool wandering = true;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float changeDirectionInterval = 3f;
    [Range(0f, 1f)][SerializeField] private float chanceToWalk = 0.6f;

    [Space(10)]
    [SerializeField] private float blendDamping = 2f;

    [Space(10)]
    [SerializeField] private float stepInterval = 0.45f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float minSpeedThreshold = 0.2f;
    [Range(0f, 1f)][SerializeField] private float volume = 0.8f;

    [Space(10)]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float maxFearThreshold = 100f;
    [SerializeField] private float fearDecayRate = 15f;

    [Space(5)]
    [SerializeField] private float noiseFearMultiplier = 20f;
    [SerializeField] private float flashlightFearRate = 35f;
    [SerializeField] private float flashlightAngle = 25f;
    [SerializeField] private float fleeDuration = 5f;

    // Component References
    private AudioSource _audioSource;
    private CharacterController _characterController;
    private SceneBlackboard _sceneBlackboard;
    private PlayerDialog _playerDialog;
    private Transform _playerTransform;

    // State Variables
    private bool _initialized;
    private string _cachedId;
    private Vector3 _moveDirection = Vector3.zero;
    private Vector3 _verticalVelocity = Vector3.zero;

    private float _targetBlend;
    private float _currentBlend;
    private float _stepTimer;
    private int _lastPlayedIndex = -1;

    private DeerState _currentState = DeerState.Wandering;
    private float _currentFear;
    private float _fleeTimer;

    private bool _shouldFlee;
    private bool _fed;

    // Cached Animator Hashes (Performance optimization over Dictionary)
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int BlendHash = Animator.StringToHash("Blend");

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard, PlayerDialog playerDialog)
    {
        _playerDialog = playerDialog;
        _playerTransform = _playerDialog.transform;
        _sceneBlackboard = sceneBlackboard;
        _cachedId = gameObject.name.ToLower();

        // Simplified event subscriptions
        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Deer.Interactable, () =>
            Interactable = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Deer.Interactable));

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Deer.CanFlee, () =>
            _shouldFlee = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Deer.CanFlee));

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Deer.CanWander, () =>
            wandering = _sceneBlackboard.Get<bool>(SceneBlackboardKeys.Deer.CanWander));

        _sceneBlackboard.ListenTo(SceneBlackboardKeys.Deer.Fed, () =>
        {
            if (_sceneBlackboard.Get<int>(SceneBlackboardKeys.Deer.Fed) == 0)
                _fed = false;
        });

        StartCoroutine(RoutineRandomizeDirection());

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} ({_cachedId}) initialized with dependencies: {sceneBlackboard.GetType().Name} | {playerDialog.GetType().Name}");
    }

    public void Interact()
    {
        if (!_initialized || _currentState == DeerState.Fleeing)
            return;

        if (_fed)
        {
            _playerDialog.ExecuteDialog("I already fed this one");
            return;
        }

        _fed = true;
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Fed, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Deer.Fed) + 1);
        _playerDialog.ExecuteDialog("What a big guy!");
    }

    public void SetInteractPrompt(string text) => interactionPrompt = text;

    public string GetInteractPrompt() => interactionPrompt;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        EvaluatePlayerAwareness();
        HandleStateLogic();

        // Calculate horizontal speed once per frame to share between movement and audio
        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        HandleMovementAndAnimation(currentSpeed);
        HandleFootsteps(currentSpeed);
    }

    private void EvaluatePlayerAwareness()
    {
        if (_playerTransform == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            float noise = _sceneBlackboard.Get<float>(SceneBlackboardKeys.Player.NoiseScore);
            float noiseFear = (noise * noiseFearMultiplier) / Mathf.Max(distanceToPlayer, 1f);

            _currentFear += noiseFear * Time.deltaTime;

            if (_sceneBlackboard.Get<bool>(SceneBlackboardKeys.Player.Flashlight.IsEnabled))
            {
                Vector3 directionToDeer = (transform.position - _playerTransform.position).normalized;
                float angle = Vector3.Angle(_playerTransform.forward, directionToDeer);

                if (angle <= flashlightAngle)
                    _currentFear += flashlightFearRate * Time.deltaTime;
            }
        }

        _currentFear = Mathf.Clamp(_currentFear - (fearDecayRate * Time.deltaTime), 0f, maxFearThreshold);

        if (_shouldFlee && _currentFear >= maxFearThreshold && _currentState != DeerState.Fleeing)
        {
            _currentState = DeerState.Fleeing;
            _fleeTimer = fleeDuration;
            _targetBlend = 1f;

            Debug.Log($"{_cachedId} is now fleeing!");
        }
    }

    private void HandleStateLogic()
    {
        if (_currentState != DeerState.Fleeing)
            return;

        _fleeTimer -= Time.deltaTime;

        if (_playerTransform != null)
        {
            Vector3 runAwayDir = (transform.position - _playerTransform.position).normalized;
            _moveDirection = new Vector3(runAwayDir.x, 0f, runAwayDir.z);
        }

        if (_fleeTimer <= 0f)
        {
            _currentState = DeerState.Wandering;
            _currentFear = 0f;
            _moveDirection = Vector3.zero;
        }
    }

    private void HandleMovementAndAnimation(float currentSpeed)
    {
        float currentMoveSpeed = _currentState == DeerState.Fleeing ? runSpeed : walkSpeed;

        if (_characterController.isGrounded && _verticalVelocity.y < 0)
            _verticalVelocity.y = -2f;

        _verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 finalVelocity = (_moveDirection * currentMoveSpeed) + _verticalVelocity;
        _characterController.Move(finalVelocity * Time.deltaTime);

        if (_moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        _currentBlend = Mathf.MoveTowards(_currentBlend, _targetBlend, blendDamping * Time.deltaTime);
        animator.SetFloat(BlendHash, _currentBlend);

        animator.SetBool(MovingHash, currentSpeed > minSpeedThreshold);
    }

    private void HandleFootsteps(float currentSpeed)
    {
        if (_characterController.isGrounded && currentSpeed > minSpeedThreshold)
        {
            float dynamicInterval = stepInterval * (walkSpeed / Mathf.Max(currentSpeed, 0.1f));

            _stepTimer += Time.deltaTime;

            if (_stepTimer >= dynamicInterval)
            {
                PlayRandomFootstep();
                _stepTimer = 0f;
            }
        }
        else
        {
            _stepTimer = stepInterval;
        }
    }

    private void PlayRandomFootstep()
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
        {
            randomIndex = 0;
        }

        _lastPlayedIndex = randomIndex;

        _audioSource.volume = volume;
        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        _audioSource.PlayOneShot(footstepClips[randomIndex]);
    }

    private IEnumerator RoutineRandomizeDirection()
    {
        while (true)
        {
            if (wandering && Random.value < chanceToWalk)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized;
                _moveDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
            }
            else
            {
                _moveDirection = Vector3.zero;
                _targetBlend = Random.Range(0, 2);
            }

            yield return new WaitForSeconds(changeDirectionInterval);
        }
    }
}
