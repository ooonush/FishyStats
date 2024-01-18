using System;
using FishNet.Serializing;

namespace Stats.FishNet
{
    internal class TraitsSyncChanges
    {
        private readonly Writer _writer = new Writer();
        private ushort Count { get; set; }

        private readonly ISyncTraits _syncTraits;
        private bool IsInitialized => _syncTraits.IsInitialized;

        public TraitsSyncChanges(ISyncTraits syncTraits)
        {
            _syncTraits = syncTraits;
        }
        
        private bool Dirty() => _syncTraits.Dirty();

        public void WriteSetAttributeValueOperation<TNumber>(SyncRuntimeAttribute<TNumber> attribute, TNumber value)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            WriteOperation(SyncTraitsOperation.SetAttributeValue, attribute.AttributeId);
            attribute.WriteSetValueOperation(_writer, value);
        }

        private void WriteOperation(SyncTraitsOperation operation, string id)
        {
            _writer.WriteByte((byte)operation);
            _writer.WriteString(id);
            Count++;
        }

        public void WriteSetStatBaseOperation<TNumber>(SyncRuntimeStat<TNumber> stat, TNumber value)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            WriteOperation(SyncTraitsOperation.SetStatBase, stat.StatId);
            stat.WriteSetStatBaseOperation(_writer, value);
        }

        public void WriteAddModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat, PercentageModifier modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            SyncTraitsOperation operation = modifier.ModifierType == ModifierType.Positive
                ? SyncTraitsOperation.AddPercentagePositiveModifier
                : SyncTraitsOperation.AddPercentageNegativeModifier;
            
            WriteOperation(operation, stat.StatId);
            stat.WriteAddModifierOperation(_writer, modifier.Value);
        }

        public void WriteRemoveModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat, PercentageModifier modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            SyncTraitsOperation operation = modifier.ModifierType == ModifierType.Positive
                ? SyncTraitsOperation.RemovePercentagePositiveModifier
                : SyncTraitsOperation.RemovePercentageNegativeModifier;
            
            WriteOperation(operation, stat.StatId);
            stat.WriteRemoveModifierOperation(_writer, modifier.Value);
        }

        public void WriteAddModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat,
            ConstantModifier<TNumber> modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            SyncTraitsOperation operation = modifier.ModifierType == ModifierType.Positive
                ? SyncTraitsOperation.AddConstantPositiveModifier
                : SyncTraitsOperation.AddConstantNegativeModifier;
            
            WriteOperation(operation, stat.StatId);
            stat.WriteAddModifierOperation(_writer, modifier.Value);
        }

        public void WriteRemoveModifierOperation<TNumber>(SyncRuntimeStat<TNumber> stat,
            ConstantModifier<TNumber> modifier)
            where TNumber : IStatNumber<TNumber>
        {
            if (!IsInitialized || !Dirty()) return;
            
            SyncTraitsOperation operation = modifier.ModifierType == ModifierType.Positive
                ? SyncTraitsOperation.RemoveConstantPositiveModifier
                : SyncTraitsOperation.RemoveConstantNegativeModifier;
            
            WriteOperation(operation, stat.StatId);
            stat.WriteRemoveModifierOperation(_writer, modifier.Value);
        }

        public void Write(Writer writer)
        {
            writer.WriteUInt16(Count);
            writer.WriteBytes(_writer.GetBuffer(), 0, _writer.Position);
        }

        public void WriteInitializeTraitsClass(string traitsClassId)
        {
            if (!IsInitialized || !Dirty()) return; 
            
            WriteOperation(SyncTraitsOperation.InitializeTraitsClass, traitsClassId);
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