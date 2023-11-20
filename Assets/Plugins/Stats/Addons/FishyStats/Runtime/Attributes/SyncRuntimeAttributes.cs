using System;
using System.Collections;
using System.Collections.Generic;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttributes : IRuntimeAttributes
    {
        private readonly NetworkTraits _networkTraits;
        private readonly Dictionary<string, ISyncRuntimeAttribute> _attributes = new();

        public int Count => _attributes.Count;

        internal SyncRuntimeAttributes(NetworkTraits networkTraits)
        {
            _networkTraits = networkTraits;
        }

        internal void SyncWithTraitsClass(ITraitsClass traitsClass)
        {
            Reset();

            foreach ((string attributeId, object attribute) in traitsClass.AttributeItems)
            {
                foreach (Type attributeInterface in attribute.GetType().GetInterfaces())
                {
                    if (attributeInterface.IsGenericType && attributeInterface.GetGenericTypeDefinition() == typeof(IAttribute<>))
                    {
                        Type genericAttributeNumberType = attributeInterface.GenericTypeArguments[0];
                        Type runtimeAttributeType = typeof(SyncRuntimeAttribute<>).MakeGenericType(genericAttributeNumberType);
                        
                        object genericRuntimeAttribute = Activator.CreateInstance(runtimeAttributeType, _networkTraits, attribute);
                        
                        if (_attributes.ContainsKey(attributeId))
                        {
                            throw new Exception($"Stat with id \"{attributeId}\" already exists");
                        }
                        
                        _attributes.Add(attributeId, (ISyncRuntimeAttribute)genericRuntimeAttribute);
                    }
                }
            }
        }

        internal void InitializeStartValues()
        {
            foreach (ISyncRuntimeAttribute runtimeAttribute in _attributes.Values)
            {
                runtimeAttribute.InitializeStartValues();
            }
        }

        public SyncRuntimeAttribute<TNumber> Get<TNumber>(AttributeId<TNumber> attributeId) where TNumber : IStatNumber<TNumber>
        {
            try
            {
                return (SyncRuntimeAttribute<TNumber>)_attributes[attributeId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("AttributeType Id not found in RuntimeAttributes", nameof(attributeId),
                    exception);
            }
        }

        IRuntimeAttribute<TNumber> IRuntimeAttributes.Get<TNumber>(AttributeId<TNumber> attributeId) => Get(attributeId);


        internal ISyncRuntimeAttribute Get(string attributeId)
        {
            try
            {
                return _attributes[attributeId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("AttributeType Id not found in RuntimeAttributes", nameof(attributeId),
                    exception);
            }
        }

        public bool Contains<TNumber>(AttributeId<TNumber> attributeId)
            where TNumber : IStatNumber<TNumber>
        {
            return _attributes.ContainsKey(attributeId);
        }

        internal void Reset() => _attributes.Clear();

        public IEnumerator<IRuntimeAttribute> GetEnumerator() => _attributes.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}