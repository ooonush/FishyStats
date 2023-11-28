namespace Stats.FishNet
{
    public sealed class SyncRuntimeAttributes : RuntimeAttributesBase
    {
        private readonly NetworkTraits _networkTraits;

        internal SyncRuntimeAttributes(NetworkTraits networkTraits) : base(networkTraits)
        {
            _networkTraits = networkTraits;
        }

        protected override IRuntimeAttribute CreateRuntimeAttribute<TNumber>(IAttribute<TNumber> stat)
        {
            return new SyncRuntimeAttribute<TNumber>(_networkTraits, stat);
        }

        public new SyncRuntimeAttribute<TNumber> Get<TNumber>(AttributeId<TNumber> attributeId) where TNumber : IStatNumber<TNumber>
        {
            return (SyncRuntimeAttribute<TNumber>)base.Get(attributeId);
        }

        internal new ISyncRuntimeAttribute Get(AttributeId attributeId)
        {
            return (ISyncRuntimeAttribute)base.Get(attributeId);
        }

        internal void Reset()
        {
            Clear();
        }
    }
}