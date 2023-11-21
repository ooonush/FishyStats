using System;
using FishNet.Serializing;
using FishNet.Utility.Extension;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttribute<TNumber> : ISyncRuntimeAttribute, IRuntimeAttribute<TNumber> where TNumber : IStatNumber<TNumber>
    {
        private readonly NetworkTraits _traits;
        public AttributeId<TNumber> AttributeId { get; }
        string IRuntimeAttribute.AttributeId => AttributeId;

        public TNumber MinValue { get; }

        private SyncTraits SyncTraits => _traits.SyncTraits;

        private TNumber _value;

        private bool _initialized;

        private readonly float _startPercent;

        public SyncRuntimeStat<TNumber> MaxRuntimeStat { get; }

        IRuntimeStat<TNumber> IRuntimeAttribute<TNumber>.MaxRuntimeStat => MaxRuntimeStat;

        public TNumber Value
        {
            get
            {
                if (!_initialized)
                {
                    ((ISyncRuntimeAttribute)this).InitializeStartValues();
                }
                return _value;
            }
            set
            {
                if (!SyncTraits.CanNetworkSetValuesInternal()) return;
                SetValueLocally(value);
                
                SyncTraits.Changes.WriteSetAttributeValueOperation(this, value);
            }
        }

        public event NetworkAttributeValueChangedAction<TNumber> OnValueChanged;

        private event AttributeValueChangedAction<TNumber> OnValueChangedLocally;

        event AttributeValueChangedAction<TNumber> IRuntimeAttribute<TNumber>.OnValueChanged
        {
            add => OnValueChangedLocally += value;
            remove => OnValueChangedLocally -= value;
        }

        private event Action OnChanged;
        event Action IRuntimeAttribute.OnValueChanged
        {
            add => OnChanged += value;
            remove => OnChanged -= value;
        }

        internal SyncRuntimeAttribute(NetworkTraits traits, IAttribute<TNumber> attribute)
        {
            MinValue = attribute.MinValue;
            AttributeId = attribute.AttributeId;
            MaxRuntimeStat = traits.RuntimeStats.Get(attribute.MaxValueStat.StatId);
            
            _traits = traits;
            _startPercent = attribute.StartPercent;
            
            MaxRuntimeStat.OnStartRecalculatingLocally += MaxStartRecalculatingLocally;
        }

        internal void WriteSetValueOperation(Writer writer, TNumber value)
        {
            writer.Write(value);
        }

        void ISyncRuntimeAttribute.InitializeStartValues()
        {
            _initialized = true;
            _value = TMath.Lerp(MinValue, MaxRuntimeStat.Value, _startPercent);
        }

        void ISyncRuntimeAttribute.ReadSetValueOperation(Reader reader, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            SetValueLocally(value);
        }

        void ISyncRuntimeAttribute.WriteFull(Writer writer)
        {
            writer.Write(Value);
        }

        void ISyncRuntimeAttribute.ReadFull(Reader reader, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            SetValueLocally(value);
        }

        private void MaxStartRecalculatingLocally()
        {
            SetValueLocally(TMath.Clamp(Value, MinValue, MaxRuntimeStat.Value));
        }

        private void SetValueLocally(TNumber value)
        {
            bool asServer = AsServerInvoke;
            
            TNumber prevValue = Value;
            _value = TMath.Clamp(value, MinValue, MaxRuntimeStat.Value);
            
            if (prevValue.Equals(value)) return;
            
            foreach (IRuntimeStat runtimeStat in SyncTraits.RuntimeStats)
            {
                ((ISyncRuntimeStat)runtimeStat).RecalculateValueLocally();
            }
            
            OnValueChanged?.Invoke(AttributeId, prevValue, Value, asServer);
            
            if (asServer && _traits.IsHost)
            {
                SyncTraits.AddClientHostChange(InvokeOnClientHostChange);
            }
            
            OnChanged?.Invoke();
            OnValueChangedLocally?.Invoke(AttributeId, prevValue, Value);
            
            return;
            
            void InvokeOnClientHostChange()
            {
                OnValueChanged?.Invoke(AttributeId, prevValue, Value, false);
            }
        }

        private bool DoubleLogic(bool asServer) => SyncTraits.NetworkManager.DoubleLogic(asServer);

        private bool AsServerInvoke => !SyncTraits.IsNetworkInitialized || SyncTraits.NetworkBehaviour.IsServer;
    }
}