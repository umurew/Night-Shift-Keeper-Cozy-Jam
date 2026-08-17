using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneBlackboard", menuName = "Scriptable Objects/Scene Blackboard")]
public class SceneBlackboard : ScriptableObject
{
    private readonly Dictionary<string, object> _stateDictionary = new();

    public event Action<string, object> OnStateChanged;

    public void Set(string key, object value)
    {
        if (_stateDictionary.TryGetValue(key, out object oldValue) && Equals(oldValue, value))
            return;

        _stateDictionary[key] = value;
        OnStateChanged?.Invoke(key, value);
        Debug.Log($"State set with the key \"{key}\" to {value}");
    }

    public T Get<T>(string key)
    {
        if (_stateDictionary.TryGetValue(key, out object value))
        {
            if (value is T typedValue)
                return typedValue;

            Debug.LogError($"State with key \"{key}\" is of type {value.GetType().Name}, but requested as {typeof(T).Name}.");
            return default;
        }

        Debug.Log($"State with the key \"{key}\" was read but not found. Returning default.");
        return default;
    }

    public bool TryGet<T>(string key, out T result)
    {
        if (_stateDictionary.TryGetValue(key, out object value) && value is T typedValue)
        {
            result = typedValue;
            return true;
        }

        result = default;
        return false;
    }

    public void ResetStates()
    {
        _stateDictionary.Clear();
    }

    private void OnEnable() => ResetStates();
}