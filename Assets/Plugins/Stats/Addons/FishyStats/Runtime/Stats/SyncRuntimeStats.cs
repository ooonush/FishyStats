using System;
using System.Collections;
using System.Collections.Generic;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeStats : IRuntimeStats
    {
        private readonly NetworkTraits _networkTraits;
        private readonly Dictionary<string, ISyncRuntimeStat> _stats = new();

        public int Count => _stats.Count;

        internal SyncRuntimeStats(NetworkTraits networkTraits)
        {
            _networkTraits = networkTraits;
        }

        public SyncRuntimeStat<TNumber> Get<TNumber>(StatId<TNumber> statId) where TNumber : IStatNumber<TNumber>
        {
            try
            {
                return (SyncRuntimeStat<TNumber>)_stats[statId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("StatType not found in RuntimeStats", nameof(statId), exception);
            }
        }

        public bool Contains<TNumber>(StatId<TNumber> statId) where TNumber : IStatNumber<TNumber>
        {
            return _stats.ContainsKey(statId);
        }

        internal void SyncWithTraitsClass(ITraitsClass traitsClass)
        {
            Reset();
            
            foreach ((string statId, object stat) in traitsClass.StatItems)
            {
                foreach (Type statInterface in stat.GetType().GetInterfaces())
                {
                    if (statInterface.IsGenericType && statInterface.GetGenericTypeDefinition() == typeof(IStat<>))
                    {
                        Type genericStatNumberType = statInterface.GenericTypeArguments[0];
                        Type runtimeStat = typeof(SyncRuntimeStat<>).MakeGenericType(genericStatNumberType);
                        
                        object genericRuntimeStat = Activator.CreateInstance(runtimeStat, _networkTraits, stat);
                        
                        if (_stats.ContainsKey(statId))
                        {
                            throw new Exception($"Stat with id \"{statId}\" already exists");
                        }
                        
                        _stats[statId] = (ISyncRuntimeStat)genericRuntimeStat;
                    }
                }
            }
        }

        internal ISyncRuntimeStat Get(string statId)
        {
            try
            {
                return _stats[statId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("StatType not found in RuntimeStats", nameof(statId), exception);
            }
        }

        internal void Reset()
        {
            _stats.Clear();
        }

        internal void InitializeStartValues()
        {
            foreach (ISyncRuntimeStat runtimeStat in _stats.Values)
            {
                runtimeStat.InitializeStartValues();
            }
        }

        IRuntimeStat<TNumber> IRuntimeStats.Get<TNumber>(StatId<TNumber> statId) => Get(statId);
        public IEnumerator<IRuntimeStat> GetEnumerator() => _stats.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}