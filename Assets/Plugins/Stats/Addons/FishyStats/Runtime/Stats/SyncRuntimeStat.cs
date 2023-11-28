using FishNet.Serializing;
using FishNet.Utility.Extension;

namespace Stats.FishNet
{
    public sealed class SyncRuntimeStat<TNumber> : RuntimeStatBase<TNumber>, ISyncRuntimeStat
        where TNumber : IStatNumber<TNumber>
    {
        private readonly NetworkTraits _traits;

        private SyncTraits SyncTraits => _traits.SyncTraits;

        private bool AsServerInvoke => !SyncTraits.IsNetworkInitialized || SyncTraits.NetworkBehaviour.IsServer;

        private TraitsSyncChanges Changes => SyncTraits.Changes;

        public override TNumber Base
        {
            get => base.Base;
            set
            {
                if (!SyncTraits.CanNetworkSetValuesInternal()) return;
                TNumber prev = Base;
                SetBaseLocally(value);
                if (!prev.Equals(Base) && AsServerInvoke)
                {
                    Changes.WriteSetStatBaseOperation(this, value);
                }
            }
        }

        private void SetBaseLocally(TNumber value)
        {
            base.Base = value;
        }

        public new event NetworkStatValueChangedAction<TNumber> OnValueChanged;

        public SyncRuntimeStat(NetworkTraits traits, IStat<TNumber> stat) : base(traits, stat)
        {
            _traits = traits;
            base.OnValueChanged += OnChanged;
        }

        private void OnChanged(StatId<TNumber> statId, TNumber prevValue, TNumber nextValue)
        {
            OnValueChanged?.Invoke(StatId, prevValue, nextValue, AsServerInvoke);

            if (!AsServerInvoke || !_traits.IsHost) return;

            SyncTraits.AddClientHostChange(InvokeOnClientHostChange);
            return;

            void InvokeOnClientHostChange()
            {
                OnValueChanged?.Invoke(StatId, prevValue, nextValue, false);
            }
        }

        #region Modifiers

        public override void AddModifier(ConstantModifier<TNumber> modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return;
            base.AddModifier(modifier);
            
            Changes.WriteAddModifierOperation(this, modifier);
        }

        public override void AddModifier(PercentageModifier modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return;
            base.AddModifier(modifier);
            
            Changes.WriteAddModifierOperation(this, modifier);
        }

        public override bool RemoveModifier(ConstantModifier<TNumber> modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return false;
            bool isRemoved = base.RemoveModifier(modifier);
            
            if (!isRemoved) return false;
            
            Changes.WriteRemoveModifierOperation(this, modifier);
            return true;

        }

        public override bool RemoveModifier(PercentageModifier modifier)
        {
            if (!SyncTraits.CanNetworkSetValuesInternal()) return false;
            bool isRemoved = base.RemoveModifier(modifier);
            
            if (!isRemoved) return false;
            
            Changes.WriteRemoveModifierOperation(this, modifier);
            
            return true;
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
            writer.Write(Base);
            writer.WriteUInt16((ushort)PercentageModifiers.Count);
            
            foreach (PercentageModifier modifier in PercentageModifiers)
            {
                writer.WriteBoolean(modifier.ModifierType == ModifierType.Positive);
                writer.WriteSingle(modifier.Value);
            }
            
            writer.WriteUInt16((ushort)ConstantModifiers.Count);
            foreach (ConstantModifier<TNumber> modifier in ConstantModifiers)
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
            
            SetBaseWithoutNotify(reader.Read<TNumber>());
            Modifiers.Clear();
            
            for (ushort i = 0; i < reader.ReadUInt16(); i++)
            {
                ModifierType modifierType = reader.ReadBoolean() ? ModifierType.Positive : ModifierType.Negative; 
                float value = reader.ReadSingle();
                Modifiers.Add(new PercentageModifier(value, modifierType));
            }
            
            for (ushort i = 0; i < reader.ReadUInt16(); i++)
            {
                ModifierType modifierType = reader.ReadBoolean() ? ModifierType.Positive : ModifierType.Negative; 
                var value = reader.Read<TNumber>();
                Modifiers.Add(new ConstantModifier<TNumber>(value, modifierType));
            }
        }

        #endregion

        #region Operations Read

        void ISyncRuntimeStat.ReadAddConstantModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            base.AddModifier(new ConstantModifier<TNumber>(value, modifierType));
        }

        void ISyncRuntimeStat.ReadRemoveConstantModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TNumber>();
            if (DoubleLogic(asServer)) return;
            base.RemoveModifier(new ConstantModifier<TNumber>(value, modifierType));
        }

        void ISyncRuntimeStat.ReadAddPercentageModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TFloat>();
            if (DoubleLogic(asServer)) return;
            base.AddModifier(new PercentageModifier(value, modifierType));
        }

        void ISyncRuntimeStat.ReadRemovePercentageModifier(Reader reader, ModifierType modifierType, bool asServer)
        {
            var value = reader.Read<TFloat>();
            if (DoubleLogic(asServer)) return;
            base.RemoveModifier(new PercentageModifier(value, modifierType));
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