using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Stats.FishNet
{
    [AddComponentMenu("Stats/NetworkTraits")]
    public sealed class NetworkTraits : NetworkBehaviour, ITraits
    {
        [SerializeField] private TraitsClassBase _traitsClass;
        [SerializeField] private TraitsClassRegistry _traitsClassRegistry;

        [SyncObject] internal readonly SyncTraitsData SyncTraitsData = new();

        public SyncRuntimeStats RuntimeStats => SyncTraitsData.RuntimeStats;
        public SyncRuntimeAttributes RuntimeAttributes => SyncTraitsData.RuntimeAttributes;
        public RuntimeStatusEffects RuntimeStatusEffects { get; private set; }

        IRuntimeStats<IRuntimeStat> ITraits.RuntimeStats => RuntimeStats;
        IRuntimeAttributes<IRuntimeAttribute> ITraits.RuntimeAttributes => RuntimeAttributes;

        private bool _initialized;

        private void Awake()
        {
            RuntimeStatusEffects = new RuntimeStatusEffects(this);
            SyncTraitsData.Initialize(this);
            if (_traitsClass)
            {
                InitializeLocally(_traitsClass);
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            const string error = "Traits not initialized. Please set TraitsClass in the inspector or call Initialize()";
            if (!_traitsClass)
            {
                Debug.LogWarning(error);
            }
        }

        public void Initialize(TraitsClassBase traitsClass)
        {
            // if (!IsOffline)
            // {
            //     throw new InvalidOperationException("You cannot initialize traits after network started");
            // }

            if (_initialized)
            {
                throw new InvalidOperationException("Traits already initialized");
            }

            if (!traitsClass)
            {
                throw new ArgumentNullException(nameof(traitsClass), "TraitsClass cannot be null.");
            }

            if (!SyncTraitsData.SyncWithTraitsClass(traitsClass)) return;

            _initialized = true;
            _traitsClass = traitsClass;
        }

        private void OnDestroy()
        {
            RuntimeStatusEffects.Clear();
        }

        internal void InitializeLocally(string traitsClassId)
        {
            if (!_traitsClassRegistry.TryGetByGuid(traitsClassId, out TraitsClassBase traitsClass))
            {
                throw new ArgumentException("TraitsClass Id not found in TraitsClassRegistry", nameof(traitsClassId));
            }

            InitializeLocally(traitsClass);
        }

        private void InitializeLocally(TraitsClassBase traitsClass)
        {
            SyncTraitsData.SyncWithTraitsClassLocally(traitsClass);
            _initialized = true;
            _traitsClass = traitsClass;
        }
    }
}
