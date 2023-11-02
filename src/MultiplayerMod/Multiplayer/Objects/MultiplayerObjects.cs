﻿using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerMod.Multiplayer.Objects;

public class MultiplayerObjects {

    private readonly MultiplayerIdentityProvider provider;

    private Dictionary<MultiplayerId, GameObject> objects = new();

    public MultiplayerObjects(MultiplayerIdentityProvider provider) {
        this.provider = provider;
    }

    public MultiplayerId Register(MultiplayerInstance instance) {
        instance.Id ??= provider.GetNextId();
        objects[instance.Id] = instance.gameObject;
        return instance.Id;
    }

    public void Remove(MultiplayerId id) => objects.Remove(id);

    public GameObject? this[MultiplayerId id] {
        get {
            objects.TryGetValue(id, out var result);
            return result;
        }
        set {
            if (value != null)
                objects[id] = value;
            else
                objects.Remove(id);
        }
    }

    public void SynchronizeWithTracker() {
        objects = new Dictionary<MultiplayerId, GameObject>();
        var kPrefabIds = KPrefabIDTracker.Get().prefabIdMap.Values;
        foreach (var kPrefabId in kPrefabIds) {
            if (kPrefabId == null)
                return;
            var gameObject = kPrefabId.gameObject;
            var instance = gameObject.GetComponent<MultiplayerInstance>();
            instance.Id = new MultiplayerId(null, kPrefabId.InstanceID);
            Register(instance);
        }
    }

}
