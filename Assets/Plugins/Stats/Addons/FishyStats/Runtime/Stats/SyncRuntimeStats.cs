namespace Stats.FishNet
{
    public sealed class SyncRuntimeStats : RuntimeStatsBase
    {
        private readonly NetworkTraits _networkTraits;

        internal SyncRuntimeStats(NetworkTraits networkTraits) : base(networkTraits)
        {
            _networkTraits = networkTraits;
        }

        protected override IRuntimeStat CreateRuntimeStat<TNumber>(IStat<TNumber> stat)
        {
            return new SyncRuntimeStat<TNumber>(_networkTraits, stat);
        }

        public new SyncRuntimeStat<TNumber> Get<TNumber>(StatId<TNumber> statId) where TNumber : IStatNumber<TNumber>
        {
            return (SyncRuntimeStat<TNumber>)base.Get(statId);
        }

        internal new ISyncRuntimeStat Get(string statId)
        {
            return (ISyncRuntimeStat)base.Get(statId);
        }

        internal void Reset()
        {
            Clear();
        }
    }
}