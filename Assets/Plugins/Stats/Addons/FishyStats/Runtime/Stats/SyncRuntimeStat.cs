using System;
using System.Collections.Generic;
using FishNet.Serializing;
using FishNet.Utility.Extension;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeStat<TNumber> : ISyncRuntimeStat, IRuntimeStat<TNumber>
        where TNumber : IStatNumber<TNumber>
    {
        private readonly StatFormula<TNumber> _formula;
        private readonly NetworkTraits _traits;
        private SyncTraits SyncTraits => _traits.SyncTraits;
        private bool AsServerInvoke => !SyncTraits.IsNetworkInitialized || SyncTraits.NetworkBehaviour.IsServer;

        public IReadOnlyList<ConstantModifier<TNumber>> ConstantModifiers => _modifiers.Constants;
        public IReadOnlyList<PercentageModifier> PercentageModifiers => _modifiers.Percentages;

        public StatId<TNumber> StatId { get; }
        string IRuntimeStat.StatId => StatId;

        private readonly Modifiers<TNumber> _modifiers;

        private TraitsSyncChanges Changes => SyncTraits.Changes;

        public TNumber Base
        {
            get => _base;
            set
            {
                if (!SyncTraits.CanNetworkSetValuesInternal()) return;
                SetBaseLocally(value);
                
                if (AsServerInvoke)
                {
                    Changes.WriteSetStatBaseOperation(this, value);
                }
            }
        }

        private bool _initialized;

        private TNumber _value;

        public TNumber Value
        {
            get
            {
                if (!_initialized)
                {
                    ((ISyncRuntimeStat)this).InitializeStartValues();
                }
                
                return _value;
            }
            private set => _value = value;
        }

        public TNumber ModifiersValue
        {
            get
            {
                TNumber value = _formula ? _formula.Calculate(this, _traits) : _base;
                return _modifiers.Calculate(value).Subtract(value);
            }
        }

        private TNumber _base;

        public event NetworkStatValueChangedAction<TNumber> OnValueChanged;
        private event StatValueChangedAction<TNumber> OnValueChangedLocally;
        internal event Action OnStartRecalculatingLocally;
        private event Action OnChanged;

        event Action IRuntimeStat.OnChanged
        {
            add => OnChanged += value;
            remove => OnChanged -= value;
        }

        event StatValueChangedAction<TNumber> IRuntimeStat<TNumber>.OnValueChanged
        {
            add => OnValueChangedLocally += value;
            remove => OnValueChangedLocally -= value;
        }

        public SyncRuntimeStat(NetworkTraits traits, IStat<TNumber> stat)
        {
            _traits = traits;
            _modifiers = new Modifiers<TNumber>();
            StatId = stat.StatId;
            _base = stat.Base;
            _formula = stat.Formula;
        }

        void ISyncRuntimeStat.InitializeStartValues()
        {
            _initialized = true;
            Value = CalculateValue();
        }

        void ISyncRuntimeStat.RecalculateValueLocally()
        {
            bool asServer = AsServerInvoke;
            
            TNumber prevValue = Value;
            TNumber nextValue = CalculateValue();
            
            if (prevValue.Equals(nextValue)) return;
            {
                Value = nextValue;
                foreach (IRuntimeStat runtimeStat in _traits.RuntimeStats)
                {
                    if (runtimeStat != this)
                    {
                        ((ISyncRuntimeStat)runtimeStat).RecalculateValueLocally();
                    }
                }
                
                OnStartRecalculatingLocally?.Invoke();
                
                OnValueChanged?.Invoke(StatId, prevValue, Value, asServer);
                
                if (asServer && _traits.IsHost)
                {
                    void InvokeOnClientHostChange()
                    {
                        OnValueChanged?.Invoke(StatId, prevValue, Value, false);
                    }
                    
                    SyncTraits.AddClientHostChange(InvokeOnClientHostChange);
                }
                
                OnChanged?.Invoke();
                OnValueChangedLocally?.Invoke(StatId, prevValue, Value);
            }
        }

        private TNumber CalculateValue()
        {
            TNumber value = _formula ? _formula.Calculate(this, _traits) : _base;
            return _modifiers.Calculate(value);
        }

        bool ISyncRuntimeStat.RecalculateWithoutNotify()
        {
            TNumber prevValue = Value;
            TNumber nextValue = CalculateValue();
            
            if (prevValue.Equals(nextValue)) return false;
            {
                Value = nextValue;
                foreach (IRuntimeStat runtimeStat in _traits.RuntimeStats)
                {
                    if (runtimeStat != this)
                    {
                        ((ISyncRuntimeStat)runtimeStat).RecalculateWithoutNotify();
                    }
                }
            }
            
            return true;
        }

        #region Modifiers

        public void AddModifier(ConstantModifier<TNumber> modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return;
            AddModifierLocally(modifier);
            
            if (AsServerInvoke)
            {
                Changes.WriteAddModifierOperation(this, modifier);
            }
        }

        public void AddModifier(PercentageModifier modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return;
            AddModifierLocally(modifier);
            
            if (AsServerInvoke)
            {
                Changes.WriteAddModifierOperation(this, modifier);
            }
        }

        public bool RemoveModifier(ConstantModifier<TNumber> modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return false;
            bool isRemoved = RemoveModifierLocally(modifier);
            
            if (!isRemoved) return false;
            
            if (!AsServerInvoke) return true;
            Changes.WriteRemoveModifierOperation(this, modifier);
            return true;

        }

        public bool RemoveModifier(PercentageModifier modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return false;
            bool isRemoved = RemoveModifierLocally(modifier);
            
            if (!isRemoved) return false;
            
            if (!AsServerInvoke) return true;
            Changes.WriteRemoveModifierOperation(this, modifier);
            
            return true;
        }

        #endregion

        #region Locally

        private void SetBaseLocally(TNumber value)
        {
            _base = value;
            ((ISyncRuntimeStat)this).RecalculateValueLocally();
        }

        private void AddModifierLocally(ConstantModifier<TNumber> modifier)
        {
            _modifiers.Add(modifier);
            ((ISyncRuntimeStat)this).RecalculateValueLocally();
        }

        private void AddModifierLocally(PercentageModifier modifier)
        {
            _modifiers.Add(modifier);
            ((ISyncRuntimeStat)this).RecalculateValueLocally();
        }

        private bool RemoveModifierLocally(ConstantModifier<TNumber> modifier)
        {
            bool success = _modifiers.Remove(modifier);
            
            if (success)
            {
                ((ISyncRuntimeStat)this).RecalculateValueLocally();
            }
            
            return success;
        }

        private bool RemoveModifierLocally(PercentageModifier modifier)
        {
            bool success = _modifiers.Remove(modifier);
            
            if (success)
            {
                ((ISyncRuntimeStat)this).RecalculateValueLocally();
            }
            
            return success;
        }

        #endregion

        #region Operations Write

        internal void WriteSetStatBaseOperation(Writer writer, TNumber value) => writer.Write(value);

        internal void WriteAddModifierOperation(Writer writer, TFloat value) => writer.Write(value);

        internal void WriteRemoveModifierOperation(Writer writer, TFloat value) => writer.Write(value);

        internal void WriteAddModifierOperation(Writer writer, TNumber value) => writer.Write(value);

        internal void WriteRemoveModifierOperation(Writer writer, TNumber value) => writer.Write(value);

        #endregion

        #region Full Read Write

        void ISyncRuntimeStat.WriteFull(Writer writer)
        {
            writer.Write(_base);
            writer.WriteUInt16((ushort)PercentageModifiers.Count);
            
            foreach (PercentageModifier modifier in PercentageModifiers)
            {
                writer.WriteBoolean(modifier.ModifierType == ModifierType.Positive);
                writer.WriteSingle(modifier.Value);
            }
            
            writer.WriteUInt16((ushort)ConstantModifiers.Count);
            foreach (var modifier in ConstantModifiers)
            {
                writer.WriteBoolean(modifier.ModifierType == ModifierType.Positive);
                writer.Write(modifier.Value);
            }
        }

        void ISyncRuntimeStat.ReadFull(Reader reader, bool asServer)
        {
            if (DoubleLogic(asServer))
            {
                reader.Read<TNumber>();
                
                for (ushort i = 0; i < reader.ReadUInt16(); i++)
                {
                    reader.ReadBoolean();
                    reader.ReadSingle();
                }
                
                for (ushort i = 0; i < reader.ReadUInt16(); i++)
                {
                    reader.ReadBoolean();
                    reader.Read<TNumber>();
                }
                return;
            }
            
            _base = reader.Read<TNumber>();
            _modifiers.Clear();
            
            for (ushort i = 0; i < reader.ReadUInt16(); i++)
            {
                ModifierType modifierType = reader.ReadBoolean() ? ModifierType.Positive : ModifierType.Negative; 
                float value = reader.ReadSingle();
                _modifiers.Add(new PercentageModifier(value, modifierType));
            }
            
            for (ushort i = 0; i < reader.ReadUInt16(); i++)
            {
                ModifierType modifierType = reader.ReadBoolean() ? ModifierType.Positive : ModifierType.Negative; 
                var value = reader.Read<TNumber>();
                _modifiers.Add(new ConstantModifier<TNumber>(value, modifierType));
            }
        }

        #endregion

        #region Operations Read

        void ISyncRuntimeStat.ReadAddConstantModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            AddModifierLocally(new ConstantModifier<TNumber>(value, modifierType));
        }

        void ISyncRuntimeStat.ReadRemoveConstantModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            RemoveModifierLocally(new ConstantModifier<TNumber>(value, modifierType));
        }

        void ISyncRuntimeStat.ReadAddPercentageModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TFloat>();
            if (DoubleLogic(asServer)) return;
            AddModifierLocally(new PercentageModifier(value, modifierType));
        }

        void ISyncRuntimeStat.ReadRemovePercentageModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TFloat>();
            if (DoubleLogic(asServer)) return;
            RemoveModifierLocally(new PercentageModifier(value, modifierType));
        }

        void ISyncRuntimeStat.ReadSetStatBaseOperation(Reader reader, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            SetBaseLocally(value);
        }

        #endregion

        private bool DoubleLogic(bool asServer) => SyncTraits.NetworkManager.DoubleLogic(asServer);
    }
}