using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneBlackboard", menuName = "Scriptable Objects/Scene Blackboard")]
public class SceneBlackboard : ScriptableObject
{
    private readonly Dictionary<string, object> _stateDictionary = new();
    private readonly Dictionary<string, Action> _keyEventDictionary = new();

    public event Action<string, object> OnStateChanged;

    public void Set(string key, object value)
    {
        //if (_stateDictionary.TryGetValue(key, out object oldValue) && Equals(oldValue, value))
        //    return;

        _stateDictionary[key] = value;
        OnStateChanged?.Invoke(key, value);

        if (_keyEventDictionary.TryGetValue(key, out var keyEvent))
            keyEvent?.Invoke();

        Debug.Log($"State set with the key \"{key}\" to \"{value}\"");
    }

    public void ListenTo(string key, Action callback)
    {
        if (!_keyEventDictionary.ContainsKey(key))
        {
            _keyEventDictionary[key] = null;
        }
        _keyEventDictionary[key] += callback;
    }

    public void RemoveListener(string key, Action callback)
    {
        if (_keyEventDictionary.ContainsKey(key))
        {
            _keyEventDictionary[key] -= callback;
            if (_keyEventDictionary[key] == null)
            {
                _keyEventDictionary.Remove(key);
            }
        }
    }

    public void InvokeOn(string key)
    {
        if (_keyEventDictionary.TryGetValue(key, out var keyEvent))
        {
            keyEvent?.Invoke();
        }
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
        _keyEventDictionary.Clear();
    }

    public async Task WaitUntilKeyExists(string key)
    {
        if (_stateDictionary.ContainsKey(key))
            return;

        Debug.Log($"Thread waiting for the key \"{key}\"");

        var tcs = new TaskCompletionSource<bool>();

        void OnChanged(string updatedKey, object value)
        {
            if (updatedKey == key)
            {
                OnStateChanged -= OnChanged;
                tcs.TrySetResult(true);
            }
        }

        OnStateChanged += OnChanged;

        await tcs.Task;
    }

    public async Task<T> WaitUntilKeyMatches<T>(string key, T expectedValue)
    {
        if (TryGet(key, out T currentValue) && Equals(currentValue, expectedValue))
            return currentValue;

        Debug.Log($"Thread waiting for the \"{key}\" to match \"{expectedValue}\"");

        var tcs = new TaskCompletionSource<T>();

        void OnChanged(string updatedKey, object value)
        {
            if (updatedKey == key && value is T typedValue && Equals(typedValue, expectedValue))
            {
                OnStateChanged -= OnChanged;
                tcs.TrySetResult(typedValue);
            }
        }

        OnStateChanged += OnChanged;

        return await tcs.Task;
    }

    private void OnEnable() => ResetStates();
}