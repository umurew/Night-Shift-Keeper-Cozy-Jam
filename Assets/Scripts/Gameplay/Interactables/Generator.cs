using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Generator : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    [SerializeField] private string interactionPrompt;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float targetLightIntensity = 2.0f;
    [SerializeField] private float targetAudioVolume = 0.5f;

    [Space(10)]
    [SerializeField] private AudioClip generatorStartingClip;
    [SerializeField] private AudioClip generatorRunningClip;

    private SceneBlackboard _sceneBlackboard;
    private AudioSource _audioSource;
    private bool _initialized = false;
    private readonly List<Light> _lights = new();
    private Sequence _generatorSeq;

    public bool Interactable { get; set; }

    public void Initialize(SceneBlackboard sceneBlackboard)
    {
        _sceneBlackboard = sceneBlackboard;
        _audioSource = GetComponent<AudioSource>();

        _sceneBlackboard.ListenTo($"generator_interactable", () =>
        {
            Interactable = _sceneBlackboard.Get<bool>($"generator_interactable");
        });

        foreach (GameObject lightSource in GameObject.FindGameObjectsWithTag("LanternLight"))
        {
            if (lightSource.TryGetComponent<Light>(out Light lightComponent))
                _lights.Add(lightComponent);
        }

        DisableGenerator(true);

        Interactable = false;
        _initialized = true;

        Debug.Log($"{GetType().Name} initialized with the following dependencies: Scene Blackboard");
    }

    public void Interact()
    {
        if (!_initialized)
            return;

        _sceneBlackboard.Set("generator_running", true);
    }

    public string GetInteractPrompt() => interactionPrompt;

    public void SetInteractPrompt(string text) => interactionPrompt = text;

    public void EnableGenerator()
    {
        foreach (Light light in _lights)
        {
            light.DOKill();
            light.enabled = true;
            light.intensity = 0f;
        }

        if (_audioSource != null)
        {
            _audioSource.DOKill();
            _audioSource.enabled = true;
            _audioSource.volume = 0f;
            _audioSource.PlayOneShot(generatorStartingClip);
        }

        _generatorSeq = DOTween.Sequence();

        _generatorSeq.Append(SetLightsIntensity(targetLightIntensity * 0.4f, 0.04f));
        _generatorSeq.Append(SetLightsIntensity(0.02f, 0.06f));
        _generatorSeq.Append(SetLightsIntensity(targetLightIntensity * 0.75f, 0.03f));
        _generatorSeq.Append(SetLightsIntensity(0.08f, 0.10f));
        _generatorSeq.Append(SetLightsIntensity(targetLightIntensity * 0.3f, 0.02f));
        _generatorSeq.Append(SetLightsIntensity(0.0f, 0.12f));
        _generatorSeq.Append(SetLightsIntensity(targetLightIntensity * 1.25f, 0.05f));
        _generatorSeq.Append(SetLightsIntensity(targetLightIntensity, 0.08f));

        if (_audioSource != null)
        {
            _generatorSeq.Insert(0f, FadeAudioTo(targetAudioVolume * 0.15f, 0.2f));
            float startupDuration = generatorStartingClip != null ? generatorStartingClip.length : 0.37f;

            _generatorSeq.InsertCallback(startupDuration, () =>
            {
                _audioSource.clip = generatorRunningClip;
                _audioSource.loop = true;
                _audioSource.Play();
            });

            _generatorSeq.Insert(startupDuration, FadeAudioTo(targetAudioVolume, 0.15f, Ease.OutQuad));
        }
    }

    public void DisableGenerator(bool instantly = false)
    {
        _generatorSeq?.Kill();

        foreach (Light light in _lights)
        {
            light.DOKill();

            if (instantly)
            {
                light.intensity = 0f;
                light.enabled = false;
            }
            else
            {
                light.DOIntensity(0f, fadeDuration).OnComplete(() => light.enabled = false);
            }
        }

        if (_audioSource != null)
        {
            _audioSource.DOKill();

            if (instantly)
            {
                _audioSource.volume = 0f;
                _audioSource.Stop();
                _audioSource.enabled = false;
            }
            else
            {
                _audioSource.DOFade(0f, fadeDuration).OnComplete(() =>
                {
                    _audioSource.Stop();
                    _audioSource.enabled = false;
                });
            }
        }
    }

    private Tween SetLightsIntensity(float intensity, float duration)
    {
        Sequence lightGroup = DOTween.Sequence();
        foreach (Light l in _lights)
            lightGroup.Join(l.DOIntensity(intensity, duration));

        return lightGroup;
    }

    private Tween FadeAudioTo(float targetVolume, float duration, Ease ease = Ease.Linear)
    {
        return DOTween.To(
            () => _audioSource.volume,
            x => _audioSource.volume = x,
            targetVolume,
            duration
        ).SetEase(ease);
    }
}
