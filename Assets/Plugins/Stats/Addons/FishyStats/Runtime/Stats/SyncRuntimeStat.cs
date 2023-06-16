using System;
using System.Collections.Generic;
using FishNet.Object;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeStat : IRuntimeStat
    {
        public StatType StatType { get; }
        public IReadOnlyList<Modifier> PercentageModifiers => _modifiers.Percentages;
        public IReadOnlyList<Modifier> ConstantModifiers => _modifiers.Constants;

        public float Base
        {
            get => _base;
            set
            {
                if (!CanNetworkSetValues()) return;
                SetBaseLocally(value, AsServerInvoke);

                AddOperation(SyncTraitsOperation.SetStatBase, value, AsServerInvoke);
            }
        }

        private bool _initialized;
        private float _value;

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
            private set => _value = value;
        }

        private float CalculateValue()
        {
            float value = _formula != null ? _formula.Calculate(this, _traits) : _base;
            return _modifiers.Calculate(value);
        }

        public float ModifiersValue
        {
            get
            {
                float value = _formula != null ? _formula.Calculate(this, _traits) : _base;

                return _modifiers.Calculate(value) - value;
            }
        }

        public event NetworkStatValueChangedAction OnNetworkValueChanged;
        public event StatValueChangedAction OnValueChanged;
        internal event Action<bool> OnStartRecalculating;

        private readonly StatFormula _formula;
        private readonly NetworkTraits _traits;
        private readonly Modifiers _modifiers = new();
        private SyncTraitsData SyncTraitsData => _traits.SyncTraitsData;

        private float _base;

        private bool AsServerInvoke => !IsNetworkInitialized || NetworkBehaviour.IsServer;
        private bool IsNetworkInitialized => SyncTraitsData.IsNetworkInitialized;
        private NetworkBehaviour NetworkBehaviour => SyncTraitsData.NetworkBehaviour;

        public SyncRuntimeStat(NetworkTraits traits, StatItem statItem)
        {
            _traits = traits;
            StatType = statItem.StatType;
            _base = statItem.Base;
            _formula = statItem.Formula;
        }

        internal void InitializeStartValues()
        {
            _initialized = true;
            _value = CalculateValue();
        }

        private bool CanNetworkSetValues() => SyncTraitsData.CanNetworkSetValuesInternal();

        internal void RecalculateValueLocally(bool asServer)
        {
            float prev = Value;
            float next = CalculateValue();

            if (Math.Abs(prev - next) > float.Epsilon)
            {
                Value = next;
                foreach (SyncRuntimeStat runtimeStat in _traits.RuntimeStats)
                {
                    if (runtimeStat.StatType != StatType)
                    {
                        runtimeStat.RecalculateValueLocally(asServer);
                    }
                }

                OnStartRecalculating?.Invoke(asServer);

                OnNetworkValueChanged?.Invoke(StatType, prev, Value, asServer);
                if (asServer && _traits.IsHost)
                {
                    SyncTraitsData.AddClientHostChange(StatType, prev, Value);
                }

                bool asClientHost = !asServer && _traits.IsHost;
                if (!asClientHost)
                {
                    OnValueChanged?.Invoke(StatType, Value - prev);
                }
            }
        }

        public void AddModifier(ModifierType modifierType, float value)
        {
            if (!CanNetworkSetValues()) return;
            AddModifierLocally(modifierType, value, AsServerInvoke);

            switch (modifierType)
            {
                case ModifierType.Constant:
                    AddOperation(SyncTraitsOperation.AddConstantModifier, value, AsServerInvoke);
                    break;
                case ModifierType.Percent:
                    AddOperation(SyncTraitsOperation.AddPercentageModifier, value, AsServerInvoke);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null);
            }
        }

        public bool RemoveModifier(ModifierType modifierType, float value)
        {
            if (!CanNetworkSetValues()) return false;
            bool isRemoved = RemoveModifierLocally(modifierType, value, AsServerInvoke);

            if (!isRemoved) return false;

            switch (modifierType)
            {
                case ModifierType.Constant:
                    AddOperation(SyncTraitsOperation.RemoveConstantModifier, value, AsServerInvoke);
                    break;
                case ModifierType.Percent:
                    AddOperation(SyncTraitsOperation.RemovePercentageModifier, value, AsServerInvoke);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null);
            }

            return true;
        }

        #region Locally

        internal void SetBaseLocally(float value, bool asServer)
        {
            _base = value;
            RecalculateValueLocally(asServer);
        }

        internal void AddModifierLocally(ModifierType modifierType, float value, bool asServer)
        {
            _modifiers.Add(modifierType, value);
            RecalculateValueLocally(asServer);
        }

        internal bool RemoveModifierLocally(ModifierType modifierType, float value, bool asServer)
        {
            bool success = _modifiers.Remove(modifierType, value);

            if (success)
            {
                RecalculateValueLocally(asServer);
            }

            return success;
        }

        #endregion

        internal void InvokeClientHostChanged(float prev, float next)
        {
            OnNetworkValueChanged?.Invoke(StatType, prev, next, false);
        }

        private void AddOperation(SyncTraitsOperation operation, float value, bool asServer)
        {
            SyncTraitsData.AddOperation(operation, StatType.Id, value, asServer);
        }

        internal void CopyDataFrom(float baseValue, Modifiers modifiers)
        {
            _base = baseValue;
            _modifiers.CopyDataFrom(modifiers);
        }
    }
}
