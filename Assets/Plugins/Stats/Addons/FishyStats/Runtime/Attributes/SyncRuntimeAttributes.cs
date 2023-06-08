using System;
using System.Collections;
using System.Collections.Generic;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttributes : IRuntimeAttributes<SyncRuntimeAttribute>
    {
        private readonly SyncTraitsData _syncTraitsData;
        private readonly Dictionary<string, SyncRuntimeAttribute> _attributes = new();

        public event NetworkAttributeValueChangedAction OnNetworkValueChanged;
        private event AttributeValueChangedAction OnLocalValueChanged;

        public int Count => _attributes.Values.Count;

        event AttributeValueChangedAction IRuntimeAttributes<SyncRuntimeAttribute>.OnValueChanged
        {
            add => OnLocalValueChanged += value;
            remove => OnLocalValueChanged -= value;
        }

        internal SyncRuntimeAttributes(SyncTraitsData syncTraitsData)
        {
            _syncTraitsData = syncTraitsData;
        }

        internal void SyncWithTraitsClass(TraitsClassBase traitsClass)
        {
            ClearAttributes();

            foreach (AttributeItem attributeItem in traitsClass.AttributeItems)
            {
                if (attributeItem == null || !attributeItem.AttributeType)
                {
                    throw new NullReferenceException("No Attribute reference found");
                }

                AttributeType attributeType = attributeItem.AttributeType;

                if (_attributes.ContainsKey(attributeType.Id))
                {
                    throw new Exception($"Attribute with AttributeType id = '{attributeType.Id}' already exists");
                }

                var runtimeAttribute = new SyncRuntimeAttribute(_syncTraitsData.Traits, attributeItem);
                runtimeAttribute.OnNetworkValueChanged += OnNetworkValueChange;
                runtimeAttribute.OnValueChanged += OnValueChange;
                _attributes[attributeType.Id] = runtimeAttribute;
            }
        }

        private void OnValueChange(AttributeType attributeType, float change)
        {
            OnLocalValueChanged?.Invoke(attributeType, change);
        }

        private void OnNetworkValueChange(AttributeType attributeType, float prev, float next, bool asServer)
        {
            OnNetworkValueChanged?.Invoke(attributeType, prev, next, asServer);
        }

        internal SyncRuntimeAttribute Get(string attributeTypeId)
        {
            try
            {
                return _attributes[attributeTypeId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("AttributeType Id not found in RuntimeAttributes", nameof(attributeTypeId), exception);
            }
        }

        public SyncRuntimeAttribute Get(AttributeType attributeType)
        {
            if (attributeType == null) throw new ArgumentNullException(nameof(attributeType));

            try
            {
                return Get(attributeType.Id);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("AttributeType not found in RuntimeAttributes", nameof(attributeType), exception);
            }
        }

        public bool Contains(AttributeType attributeType) => _attributes.ContainsKey(attributeType.Id);

        internal void Reset() => ClearAttributes();

        public IEnumerator<SyncRuntimeAttribute> GetEnumerator() => _attributes.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void ClearAttributes()
        {
            var syncRuntimeAttributes = new List<SyncRuntimeAttribute>(_attributes.Values);
            foreach (SyncRuntimeAttribute runtimeAttribute in syncRuntimeAttributes)
            {
                runtimeAttribute.OnNetworkValueChanged -= OnNetworkValueChange;
                runtimeAttribute.OnValueChanged -= OnValueChange;
                _attributes.Remove(runtimeAttribute.AttributeType.Id);
            }
        }
    }
}