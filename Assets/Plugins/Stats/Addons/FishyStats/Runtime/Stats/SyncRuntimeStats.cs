using System;
using System.Collections;
using System.Collections.Generic;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeStats : IRuntimeStats<SyncRuntimeStat>
    {
        private readonly Dictionary<string, SyncRuntimeStat> _stats = new();
        private readonly SyncTraitsData _syncTraitsData;

        public int Count => _stats.Values.Count;

        public event NetworkStatValueChangedAction OnNetworkValueChanged;
        private event StatValueChangedAction OnLocalValueChanged;

        event StatValueChangedAction IRuntimeStats<SyncRuntimeStat>.OnValueChanged
        {
            add => OnLocalValueChanged += value;
            remove => OnLocalValueChanged -= value;
        }

        internal SyncRuntimeStats(SyncTraitsData syncTraitsData)
        {
            _syncTraitsData = syncTraitsData;
        }

        internal void SyncWithTraitsClass(IEnumerable<StatItem> statItems)
        {
            ClearStats();

            foreach (StatItem statItem in statItems)
            {
                if (statItem == null || !statItem.StatType)
                {
                    throw new NullReferenceException("No StatType reference found in TraitsClass");
                }

                StatType statType = statItem.StatType;
                if (_stats.ContainsKey(statType.Id))
                {
                    throw new Exception($"Stat with StatType \"{statType.name}\" already exists");
                }

                var syncRuntimeStat = new SyncRuntimeStat(_syncTraitsData.Traits, statItem);
                syncRuntimeStat.OnNetworkValueChanged += OnNetworkValueChange;
                syncRuntimeStat.OnValueChanged += OnValueChange;
                _stats[statType.Id] = syncRuntimeStat;
            }
        }

        private void OnNetworkValueChange(StatType statType, float prev, float next, bool asServer)
        {
            OnNetworkValueChanged?.Invoke(statType, prev, next, asServer);
        }

        private void ClearStats()
        {
            var stats = new List<SyncRuntimeStat>(_stats.Values);
            foreach (SyncRuntimeStat runtimeStat in stats)
            {
                runtimeStat.OnNetworkValueChanged -= OnNetworkValueChange;
                runtimeStat.OnValueChanged -= OnValueChange;
                _stats.Remove(runtimeStat.StatType.Id);
            }
        }

        private void OnValueChange(StatType statType, float change)
        {
            OnLocalValueChanged?.Invoke(statType, change);
        }

        internal SyncRuntimeStat Get(string statTypeId)
        {
            try
            {
                return _stats[statTypeId];
            }
            catch (Exception exception)
            {
                throw new ArgumentException("StatType Id not found in RuntimeStats", nameof(statTypeId), exception);
            }
        }

        public SyncRuntimeStat Get(StatType statType)
        {
            if (statType == null) throw new ArgumentNullException(nameof(statType));

            try
            {
                return Get(statType.Id);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("StatType not found in RuntimeStats", nameof(statType), exception);
            }
        }

        public bool Contains(StatType statType) => _stats.ContainsKey(statType.Id);

        public void Reset() => ClearStats();

        public IEnumerator<SyncRuntimeStat> GetEnumerator() => _stats.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}