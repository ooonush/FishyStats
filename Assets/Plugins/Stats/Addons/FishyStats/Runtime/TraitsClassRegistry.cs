using UnityEngine;

namespace Stats.FishNet
{
    public abstract class TraitsClassRegistry : ScriptableObject
    {
        public abstract bool TryGetByGuid(string traitsClassId, out ITraitsClass value);
    }
}