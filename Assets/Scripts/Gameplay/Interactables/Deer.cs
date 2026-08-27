using System.Collections;
using System.Collections.Generic;
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

    private PlayerNoise _playerNoise;
    private AudioSource _audioSource;
    private SceneBlackboard _sceneBlackboard;
    private CharacterController _characterController;
    private PlayerDialog _playerDialog;
    private Dictionary<string, int> _animationHashes;
    private bool _initialized = false;
    private string _cachedId;

    private Vector3 _moveDirection = Vector3.zero;
    private Vector3 _verticalVelocity = Vector3.zero;

    private float _targetBlend = 0f;
    private float _currentBlend = 0f;

    private float _stepTimer;
    private int _lastPlayedIndex = -1;

    private DeerState _currentState = DeerState.Wandering;
    private float _currentFear = 0f;
    private float _fleeTimer = 0f;
    private Transform _playerTransform;

    private bool _shouldFlee = false;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard, PlayerNoise playerNoise, PlayerDialog playerDialog)
    {
        _playerNoise = playerNoise;
        _playerDialog = playerDialog;
        _playerTransform = _playerNoise.transform;
        _sceneBlackboard = sceneBlackboard;
        _cachedId = gameObject.name.ToLower();

        _sceneBlackboard.ListenTo($"{_cachedId}_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"{_cachedId}_interactable");
        });

        _sceneBlackboard.ListenTo($"{_cachedId}_shouldFlee", () =>
        {
            _shouldFlee = _sceneBlackboard.Get<bool>($"{_cachedId}_shouldFlee");
        });

        StartCoroutine(RoutineRandomizeDirection());

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} ({_cachedId}) initialized with the following dependencies: Player Noise");
    }

    public void Interact()
    {
        if (_currentState == DeerState.Fleeing)
            return;

        if (_sceneBlackboard.Get<bool>($"{_cachedId}_fed"))
        {
            _playerDialog.SetDialog("I already fed this one");
            return;
        }

        _sceneBlackboard.Set($"{_cachedId}_fed", true);
        _sceneBlackboard.Set("deers_fed", _sceneBlackboard.Get<int>("deers_fed") + 1);

        _playerDialog.SetDialog("What a big guy!");
    }

    public void SetInteractPrompt(string text) => interactionPrompt = text;

    public string GetInteractPrompt() => interactionPrompt;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;

        _animationHashes = new()
        {
            { "Moving", Animator.StringToHash("Moving") },
            { "Blend", Animator.StringToHash("Blend") }
        };
    }
    private void Update()
    {
        if (!_initialized)
            return;

        EvaluatePlayerAwareness();

        HandleStateLogic();

        HandleMovementAndAnimation();

        HandleFootsteps();
    }

    private void EvaluatePlayerAwareness()
    {
        if (_playerTransform == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            float noise = _playerNoise.CurrentNoiseLevel;
            float noiseFear = (noise * noiseFearMultiplier) / Mathf.Max(distanceToPlayer, 1f);
            _currentFear += noiseFear * Time.deltaTime;

            if (_playerNoise.IsFlashlightOn)
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
        if (_currentState == DeerState.Fleeing)
        {
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
    }

    private void HandleMovementAndAnimation()
    {
        float currentMoveSpeed = (_currentState == DeerState.Fleeing) ? runSpeed : walkSpeed;

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
        animator.SetFloat(_animationHashes["Blend"], _currentBlend);

        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
        bool moving = horizontalVelocity.magnitude > minSpeedThreshold;
        animator.SetBool(_animationHashes["Moving"], moving);
    }

    private void HandleFootsteps()
    {
        Vector3 horizontalVelocity = new(_characterController.velocity.x, 0, _characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

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
            _stepTimer = stepInterval;
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
            randomIndex = 0;

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
