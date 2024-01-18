using FishNet.Serializing;
using FishNet.Utility.Extension;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttribute<TNumber> : RuntimeAttributeBase<TNumber>, ISyncRuntimeAttribute where TNumber : IStatNumber<TNumber>
    {
        private readonly NetworkTraits _traits;
        private SyncTraits SyncTraits => _traits.SyncTraits;

        public new SyncRuntimeStat<TNumber> MaxRuntimeStat { get; }

        public override TNumber Value
        {
            get => base.Value;
            set
            {
                if (!SyncTraits.CanNetworkSetValuesInternal()) return;
                SetValueLocally(value);
                
                SyncTraits.Changes.WriteSetAttributeValueOperation(this, value);
            }
        }

        public new event NetworkAttributeValueChangedAction<TNumber> OnValueChanged;

        internal SyncRuntimeAttribute(NetworkTraits traits, IAttribute<TNumber> attribute) : base(traits, attribute)
        {
            MaxRuntimeStat = traits.RuntimeStats.Get(attribute.MaxValueStat.StatId);
            _traits = traits;
            
            base.OnValueChanged += OnChanged;
        }

        private void OnChanged(AttributeId<TNumber> attributeId, TNumber prevValue, TNumber nextValue)
        {
            OnValueChanged?.Invoke(AttributeId, prevValue, nextValue, AsServerInvoke);
            if (AsServerInvoke && _traits.IsHostInitialized)
            {
                SyncTraits.AddClientHostChange(InvokeOnClientHostChange);
            }
            
            return;
            
            void InvokeOnClientHostChange()
            {
                OnValueChanged?.Invoke(AttributeId, prevValue, nextValue, false);
            }
        }

        internal void WriteSetValueOperation(Writer writer, TNumber value)
        {
            writer.Write(value);
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

        private void SetValueLocally(TNumber value)
        {
            base.Value = value;
        }

        private bool DoubleLogic(bool asServer) => _traits.DoubleLogic(asServer);

        private bool AsServerInvoke => !SyncTraits.IsNetworkInitialized || _traits.IsServerInitialized;
    }
}