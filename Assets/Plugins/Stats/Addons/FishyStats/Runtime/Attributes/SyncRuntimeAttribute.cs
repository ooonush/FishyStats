using System;
using UnityEngine;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttribute : IRuntimeAttribute
    {
        private readonly NetworkTraits _traits;
        public AttributeType AttributeType => _attributeItem.AttributeType;
        private readonly AttributeItem _attributeItem;
        public float MinValue => _attributeItem.MinValue;
        private SyncTraitsData SyncTraitsData => _traits.SyncTraitsData;

        public float MaxValue => _attributeItem.MaxValueType != null ? _traits.RuntimeStats.Get(_attributeItem.MaxValueType).Value : 0f;

        private float _value;
        private bool _initialized;

        public float Value
        {
            get
            {
                if (!_initialized)
                {
                    InitializeStartValues();
                }
                return _value;
            }
            set
            {
                if (!SyncTraitsData.CanNetworkSetValuesInternal()) return;
                bool asServerInvoke = !SyncTraitsData.IsNetworkInitialized || SyncTraitsData.NetworkBehaviour.IsServer;
                SetValueLocally(value, asServerInvoke);

                SyncTraitsData.AddOperation(SyncTraitsOperation.SetAttributeValue, AttributeType.Id, value,
                    asServerInvoke);
            }
        }

        public float Ratio => (Value - MinValue) / (MaxValue - MinValue);

        public event NetworkAttributeValueChangedAction OnNetworkValueChanged;
        public event AttributeValueChangedAction OnValueChanged;

        internal SyncRuntimeAttribute(NetworkTraits traits, AttributeItem attributeItem)
        {
            _traits = traits;
            _attributeItem = attributeItem;
            if (_attributeItem.MaxValueType)
            {
                _traits.RuntimeStats.Get(_attributeItem.MaxValueType).OnStartRecalculating += OnMaxValueChanged;
            }
        }

        internal void InitializeStartValues()
        {
            _initialized = true;
            _value = Mathf.Lerp(MinValue, MaxValue, _attributeItem.StartPercent);
        }

        private void OnMaxValueChanged(bool asServer)
        {
            if (!asServer && _traits.IsHost) return;
            SetValueLocally(Math.Clamp(Value, MinValue, MaxValue), asServer);
        }

        internal void SetValueLocally(float value, bool asServer)
        {
            float prevValue = Value;
            _value = Math.Clamp(value, MinValue, MaxValue);

            if (Math.Abs(prevValue - Value) > float.Epsilon)
            {
                foreach (SyncRuntimeStat runtimeStat in SyncTraitsData.RuntimeStats)
                {
                    runtimeStat.RecalculateValueLocally(asServer);
                }

                OnNetworkValueChanged?.Invoke(AttributeType, prevValue, Value, asServer);
                if (asServer && _traits.IsHost)
                {
                    SyncTraitsData.AddClientHostChange(AttributeType, prevValue, Value);
                }

                bool asClientHost = !asServer && _traits.IsHost;
                if (!asClientHost)
                {
                    OnValueChanged?.Invoke(AttributeType, Value - prevValue);
                }
            }
        }

        internal void InvokeClientHostChange(float prev, float next)
        {
            OnNetworkValueChanged?.Invoke(AttributeType, prev, next, false);
        }

        internal void CopyDataFrom(float value)
        {
            _value = value;
        }

        internal void InvokeClientHostChanged(float prev, float next)
        {
            OnNetworkValueChanged?.Invoke(AttributeType, prev, next, false);
        }
    }
}