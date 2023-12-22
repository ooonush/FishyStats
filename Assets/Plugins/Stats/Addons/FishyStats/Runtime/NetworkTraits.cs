using System;
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

        [SyncObject] internal readonly SyncTraits SyncTraits = new();
        internal bool IsInitialized { get; private set; }
        public event Action OnInitialized;

        private bool _onStartNetworkCalled;
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
            
            if (_traitsClass.Value == null) return;
            InitializeInternal(_traitsClass.Value);
        }

        public override void OnStartNetwork()
        {
            _onStartNetworkCalled = true;
            const string warning = "Traits not initialized. Please set TraitsClass in the inspector or call Initialize()";
            if (_traitsClass.Value == null)
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

            if (IsInitialized)
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
            if (IsInitialized)
            {
                throw new InvalidOperationException("NetworkTraits is already initialized");
            }

            _traitsClass = new TraitsClassItem(traitsClass);
            InitializeInternal(traitsClass);
        }

        private void InitializeInternal(ITraitsClass traitsClass)
        {
            IsInitialized = true;
            this.SyncWithTraitsClass(traitsClass);
            if (_onStartNetworkCalled)
            {
                OnInitialized?.Invoke();
            }
        }
    }
}
