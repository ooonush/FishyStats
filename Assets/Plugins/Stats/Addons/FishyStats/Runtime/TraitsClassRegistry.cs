using System;
using UnityEngine;

namespace Stats.FishNet
{
    [CreateAssetMenu(menuName = "Stats/TraitsClass Registry", fileName = "TraitsClass Registry", order = 0)]
    public sealed class TraitsClassRegistry : ScriptableObject
    {
        [SerializeField] private TraitsClassBase[] _traitsClasses;

        public bool TryGetByGuid(string guid, out TraitsClassBase value)
        {
            value = Array.Find(_traitsClasses, avatar => avatar.Id == guid);
            return value != null;
        }
    }
}