using UnityEngine;

public abstract class ServiceBase : MonoBehaviour, IService
{
    public bool IsInitialized { get; private set; }

    public virtual void Initialize()
    {
        if (IsInitialized)
        {
            Debug.LogWarning($"{GetType().Name} is already initialized.", this);
            return;
        }

        OnInitialize();
        IsInitialized = true;
    }

    protected abstract void OnInitialize();
}
