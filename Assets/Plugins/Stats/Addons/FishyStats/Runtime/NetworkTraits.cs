using System;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Stats.FishNet
{
    [AddComponentMenu("Stats/NetworkTraits")]
    public sealed class NetworkTraits : NetworkBehaviour, ITraits
    {
        public TraitsClassRegistry TraitsClassRegistry;

        [SerializeField] private TraitsClassItem _traitsClass;

        internal readonly SyncTraits SyncTraits = new();
        public bool IsInitialized => _isSyncedWithTraitsClass && !IsOffline;
        public event Action OnInitialized;
        private bool _isSyncedWithTraitsClass;
        internal ITraitsClass TraitsClass => _traitsClass.Value;

        public SyncRuntimeStats RuntimeStats { get; private set; }
        public SyncRuntimeAttributes RuntimeAttributes { get; private set; }
        public RuntimeStatusEffects RuntimeStatusEffects { get; private set; }

        IRuntimeStats ITraits.RuntimeStats => RuntimeStats;
        IRuntimeAttributes ITraits.RuntimeAttributes => RuntimeAttributes;

        private void Awake()
        {
            RuntimeStats = new SyncRuntimeStats(this);
            RuntimeAttributes = new SyncRuntimeAttributes(this);
            RuntimeStatusEffects = new RuntimeStatusEffects(this);
            
            SyncTraits.Initialize(this);
            
            if (TraitsClass == null) return;
            InitializeInternal(TraitsClass);
        }

        public override void OnStartNetwork()
        {
            const string warning = "Traits not initialized. Please set TraitsClass in the inspector or call Initialize()";
            if (TraitsClass == null)
            {
                NetworkManager.LogWarning(warning);
            }
            else
            {
                OnInitialized?.Invoke();
            }
        }

        public void Initialize(ITraitsClass traitsClass)
        {
            if (traitsClass == null)
            {
                throw new ArgumentNullException(nameof(traitsClass), "TraitsClass cannot be null.");
            }

            if (_isSyncedWithTraitsClass)
            {
                throw new InvalidOperationException("NetworkTraits is already initialized.");
            }

            // if (!IsOffline)
            // {
            //     throw new InvalidOperationException("NetworkTraits cannot be initialized when network is started.");
            // }

            if (SyncTraits.UpdateTraitsClass(traitsClass))
            {
                InitializeLocally(traitsClass);
            }
        }

        public void Initialize(string traitsClassId)
        {
            if (TraitsClassRegistry.TryGetByGuid(traitsClassId, out ITraitsClass traitsClass))
            {
                Initialize(traitsClass);
            }
            else
            {
                throw new ArgumentException("TraitsClass Id not found in TraitsClassRegistry", nameof(traitsClassId));
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                RuntimeStatusEffects.Update();
            }
        }

        private void OnDestroy()
        {
            RuntimeStatusEffects.Clear();
        }

        internal void InitializeLocally(string traitsClassId)
        {
            if (!TraitsClassRegistry.TryGetByGuid(traitsClassId, out ITraitsClass traitsClass))
            {
                throw new ArgumentException("TraitsClass Id not found in TraitsClassRegistry", nameof(traitsClassId));
            }

            InitializeLocally(traitsClass);
        }

        private void InitializeLocally(ITraitsClass traitsClass)
        {
            if (_isSyncedWithTraitsClass)
            {
                throw new InvalidOperationException("NetworkTraits is already initialized");
            }

            _traitsClass = new TraitsClassItem(traitsClass);
            InitializeInternal(traitsClass);
        }

        private void InitializeInternal(ITraitsClass traitsClass)
        {
            this.SyncWithTraitsClass(traitsClass);
            _isSyncedWithTraitsClass = true;
        }
    }
}
