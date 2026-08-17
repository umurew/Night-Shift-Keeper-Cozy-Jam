using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectDataRegistry", menuName = "Scriptable Objects/Object Data Registry")]
public class ObjectDataRegistry : ScriptableObject
{
    [Serializable]
    public struct ObjectData
    {
        public string Id;
        public GameObject Prefab;
        public ObjectDataType Type;
    }

    [SerializeField] private List<ObjectData> items = new();

    private Dictionary<string, GameObject> _prefabCache;
    private Dictionary<string, ObjectDataType> _typeCache;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            Debug.LogWarning($"{GetType().Name} is already initialized.");
            return;
        }

        _prefabCache = new(StringComparer.OrdinalIgnoreCase);
        _typeCache = new(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Id))
                continue;

            if (item.Prefab != null)
                _prefabCache[item.Id] = item.Prefab;

            _typeCache[item.Id] = item.Type;
        }

        _initialized = true;
        Debug.Log($"{GetType().Name} initialized with {items.Count} object.");
    }

    public bool TryGetObjectType(string objectId, out ObjectDataType type) => _typeCache.TryGetValue(objectId, out type);

    public bool TryGetPrefab(string objectId, out GameObject gameObject) => _prefabCache.TryGetValue(objectId, out gameObject);

    public IReadOnlyList<string> GetAvailableKeys() => _prefabCache.Keys.ToList();

    private void OnEnable() => _initialized = false;
}