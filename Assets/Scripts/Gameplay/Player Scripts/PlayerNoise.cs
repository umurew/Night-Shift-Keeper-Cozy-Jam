using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlight;

    public float CurrentNoiseLevel { get; private set; }
    public bool IsFlashlightOn => flashlight != null && flashlight.enabled && flashlight.gameObject.activeInHierarchy;
    public void SetNoiseLevel(float level) => CurrentNoiseLevel = level;

    public void UpdateNoiseLevel(float speed, bool isGrounded)
    {
        if (!isGrounded || speed < 0.1f)
        {
            CurrentNoiseLevel = 0f;
            return;
        }

        CurrentNoiseLevel = speed;
    }
}
