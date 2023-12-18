using System;
using UnityEngine;

namespace Stats.FishNet
{
    [CreateAssetMenu(menuName = "Stats/TraitsClassAsset Registry", fileName = "TraitsClassAsset Registry", order = 0)]
    public sealed class TraitsClassAssetRegistry : TraitsClassRegistry
    {
        [SerializeField] private TraitsClassAsset[] _traitsClassAssets;

        public override bool TryGetByGuid(string traitsClassId, out ITraitsClass value)
        {
            value = Array.Find(_traitsClassAssets, avatar => avatar.Id == traitsClassId);
            return value != null;
        }
    }
}