using System;
using FishNet.Serializing;

namespace Stats.FishNet
{
    internal class TraitsSyncChanges
    {
        private readonly Writer _writer = new Writer();
        private ushort Count { get; set; }

        private readonly SyncTraits _syncTraits;
        private bool IsRegistered => _syncTraits.IsRegistered;

        public TraitsSyncChanges(SyncTraits syncTraits)
        {
            _syncTraits = syncTraits;
        }
        
        private bool Dirty() => _syncTraits.Dirty();

        public void WriteSetAttributeValueOperation<TNumber>(SyncRuntimeAttribute<TNumber> attribute, TNumber value)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            _writer.WriteByte((byte)SyncTraitsOperation.SetAttributeValue);
            _writer.WriteString(attribute.AttributeId);
            attribute.WriteSetValueOperation(_writer, value);
        }

        public void WriteSetStatBaseOperation<TNumber>(SyncRuntimeStat<TNumber> stat, TNumber value)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            _writer.WriteByte((byte)SyncTraitsOperation.SetStatBase);
            _writer.WriteString(stat.StatId);
            stat.WriteSetStatBaseOperation(_writer, value);
        }

        public void WriteAddModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat, PercentageModifier modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            switch (modifier.ModifierType)
            {
                case ModifierType.Positive:
                    _writer.WriteByte((byte)SyncTraitsOperation.AddPercentagePositiveModifier);
                    break;
                case ModifierType.Negative:
                    _writer.WriteByte((byte)SyncTraitsOperation.AddPercentageNegativeModifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _writer.WriteString(stat.StatId);
            stat.WriteAddModifierOperation(_writer, modifier.Value);
        }

        public void WriteRemoveModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat, PercentageModifier modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            switch (modifier.ModifierType)
            {
                case ModifierType.Positive:
                    _writer.WriteByte((byte)SyncTraitsOperation.RemovePercentagePositiveModifier);
                    break;
                case ModifierType.Negative:
                    _writer.WriteByte((byte)SyncTraitsOperation.RemovePercentageNegativeModifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _writer.WriteString(stat.StatId);
            stat.WriteRemoveModifierOperation(_writer, modifier.Value);
        }

        public void WriteAddModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat,
            ConstantModifier<TNumber> modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            switch (modifier.ModifierType)
            {
                case ModifierType.Positive:
                    _writer.WriteByte((byte)SyncTraitsOperation.AddConstantPositiveModifier);
                    break;
                case ModifierType.Negative:
                    _writer.WriteByte((byte)SyncTraitsOperation.AddConstantNegativeModifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _writer.WriteString(stat.StatId);
            stat.WriteAddModifierOperation(_writer, modifier.Value);
        }

        public void WriteRemoveModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat,
            ConstantModifier<TNumber> modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsRegistered || !Dirty()) return;
            
            switch (modifier.ModifierType)
            {
                case ModifierType.Positive:
                    _writer.WriteByte((byte)SyncTraitsOperation.RemoveConstantPositiveModifier);
                    break;
                case ModifierType.Negative:
                    _writer.WriteByte((byte)SyncTraitsOperation.RemoveConstantNegativeModifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _writer.WriteString(stat.StatId);
            stat.WriteRemoveModifierOperation(_writer, modifier.Value);
        }

        public void Write(Writer writer)
        {
            writer.WriteUInt16(Count);
            writer.WriteBytesAndSize(_writer.GetBuffer());
        }

        public void WriteInitializeTraitsClass(string traitsClassId)
        {
            if (!IsRegistered || !Dirty()) return; 
            
            _writer.WriteByte((byte)SyncTraitsOperation.InitializeTraitsClass);
            _writer.WriteString(traitsClassId);
        }

        public void ReadAll(Reader reader, bool asServer)
        {
            ushort operationsCount = reader.ReadUInt16();
            
            for (ushort i = 0; i < operationsCount; i++)
            {
                var operation = (SyncTraitsOperation)reader.ReadByte();
                string id = reader.ReadString();
                
                ReadOperation(operation, reader, id, asServer);
            }
        }

        private void ReadOperation(SyncTraitsOperation operation, Reader reader, string id, bool asServer)
        {
            switch (operation)
            {
                case SyncTraitsOperation.SetStatBase:
                    GetStat().ReadSetStatBaseOperation(reader, asServer);
                    break;
                case SyncTraitsOperation.AddConstantNegativeModifier:
                    GetStat().ReadAddConstantModifier(reader, ModifierType.Negative, asServer);
                    break;
                case SyncTraitsOperation.RemoveConstantNegativeModifier:
                    GetStat().ReadRemoveConstantModifier(reader, ModifierType.Negative, asServer);
                    break;
                case SyncTraitsOperation.AddPercentageNegativeModifier:
                    GetStat().ReadAddPercentageModifier(reader, ModifierType.Negative, asServer);
                    break;
                case SyncTraitsOperation.RemovePercentageNegativeModifier:
                    GetStat().ReadRemovePercentageModifier(reader, ModifierType.Negative, asServer);
                    break;
                case SyncTraitsOperation.AddConstantPositiveModifier:
                    GetStat().ReadAddConstantModifier(reader, ModifierType.Positive, asServer);
                    break;
                case SyncTraitsOperation.RemoveConstantPositiveModifier:
                    GetStat().ReadRemoveConstantModifier(reader, ModifierType.Positive, asServer);
                    break;
                case SyncTraitsOperation.AddPercentagePositiveModifier:
                    GetStat().ReadAddPercentageModifier(reader, ModifierType.Positive, asServer);
                    break;
                case SyncTraitsOperation.RemovePercentagePositiveModifier:
                    GetStat().ReadRemovePercentageModifier(reader, ModifierType.Positive, asServer);
                    break;
                case SyncTraitsOperation.SetAttributeValue:
                    GetAttribute().ReadSetValueOperation(reader, asServer);
                    break;
                case SyncTraitsOperation.InitializeTraitsClass:
                    _syncTraits.InitializeTraitsClass(id, asServer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return;

            ISyncRuntimeStat GetStat() => _syncTraits.RuntimeStats.Get(id);
            ISyncRuntimeAttribute GetAttribute() => _syncTraits.RuntimeAttributes.Get(id);
        }

        public void Reset()
        {
            _writer.Reset();
            Count = 0;
        }
    }
}