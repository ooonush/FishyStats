using System;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using FishNet.Serializing;

namespace Stats.FishNet
{
    internal enum SyncTraitsOperation : byte
    {
        SetStatBase,
        AddConstantModifier,
        RemoveConstantModifier,
        AddPercentageModifier,
        RemovePercentageModifier,
        SetAttributeValue
    }

    internal sealed class SyncTraitsData : SyncBase, ICustomSync
    {
        #region Types

        private enum ChangeType : byte
        {
            Stat,
            Attribute
        }

        private readonly struct CachedOnChange
        {
            public readonly ChangeType Type;
            public readonly string TypeId;
            public readonly float PrevValue;
            public readonly float Value;

            public CachedOnChange(ChangeType type, string typeId, float prevValue, float value)
            {
                Type = type;
                TypeId = typeId;
                PrevValue = prevValue;
                Value = value;
            }
        }

        private readonly struct ChangeData
        {
            public readonly SyncTraitsOperation Operation;
            public readonly float Value;
            public readonly string StatId;

            public ChangeData(SyncTraitsOperation operation, float value, string statId)
            {
                Operation = operation;
                Value = value;
                StatId = statId;
            }
        }

        #endregion

        private readonly List<CachedOnChange> _clientHostChanges = new();
        private readonly List<ChangeData> _changes = new();
        public readonly SyncRuntimeStats RuntimeStats;
        public readonly SyncRuntimeAttributes RuntimeAttributes;

        private TraitsClassBase _traitsClass;
        private TraitsClassBase _lastSyncedClass;
        internal NetworkTraits Traits;

        public SyncTraitsData()
        {
            RuntimeStats = new SyncRuntimeStats(this);
            RuntimeAttributes = new SyncRuntimeAttributes(this);
        }

        internal void AddClientHostChange(AttributeType attributeType, float prev, float next)
        {
            _clientHostChanges.Add(new CachedOnChange(ChangeType.Attribute, attributeType.Id, prev, next));
        }

        internal void AddClientHostChange(StatType statType, float prev, float next)
        {
            _clientHostChanges.Add(new CachedOnChange(ChangeType.Stat, statType.Id, prev, next));
        }

        internal void AddOperation(SyncTraitsOperation operation, string typeId, float value, bool asServer)
        {
            if (!IsRegistered || !asServer || !Dirty()) return;
            _changes.Add(new ChangeData(operation, value, typeId));
        }

        public void Initialize(NetworkTraits traits)
        {
            Traits = traits;
        }

        public void SyncWithTraitsClassLocally(TraitsClassBase traitsClass)
        {
            _traitsClass = traitsClass;
            RuntimeStats.SyncWithTraitsClass(traitsClass.StatItems);
            RuntimeAttributes.SyncWithTraitsClass(traitsClass);

            foreach (SyncRuntimeStat runtimeStat in RuntimeStats)
            {
                runtimeStat.InitializeStartValues();
            }

            foreach (SyncRuntimeAttribute runtimeAttribute in RuntimeAttributes)
            {
                runtimeAttribute.InitializeStartValues();
            }
        }

        public bool SyncWithTraitsClass(TraitsClassBase traitsClass)
        {
            if (!CanNetworkSetValues()) return false;

            SyncWithTraitsClassLocally(traitsClass);
            Dirty();

            return true;
        }

        public override void WriteDelta(PooledWriter writer, bool resetSyncTick = true)
        {
            base.WriteDelta(writer, resetSyncTick);

            writer.WriteBoolean(false); // False to write delta

            if (_lastSyncedClass != _traitsClass)
            {
                writer.WriteBoolean(true); // write classId
                WriteClassId(writer);
            }
            else
            {
                writer.WriteBoolean(false); // write classId
            }

            writer.WriteInt32(_changes.Count);
            foreach (ChangeData change in _changes)
            {
                writer.WriteByte((byte)change.Operation);
                writer.WriteString(change.StatId);
                writer.WriteSingle(change.Value);
            }

            _changes.Clear();
        }

        public override void WriteFull(PooledWriter writer)
        {
            WriteHeader(writer, false);

            writer.WriteBoolean(true); // write full

            WriteClassId(writer);
            WriteStatsFull(writer);
            WriteAttributesFull(writer);
        }

        private void WriteStatsFull(PooledWriter writer)
        {
            writer.WriteInt32(RuntimeStats.Count);

            foreach (SyncRuntimeStat runtimeStat in RuntimeStats)
            {
                writer.WriteString(runtimeStat.StatType.Id);
                writer.WriteSingle(runtimeStat.Base);
                writer.WriteInt32(runtimeStat.PercentageModifiers.Count);
                foreach (Modifier modifier in runtimeStat.PercentageModifiers)
                {
                    writer.WriteSingle(modifier.Value);
                }
                writer.WriteInt32(runtimeStat.ConstantModifiers.Count);
                foreach (Modifier modifier in runtimeStat.ConstantModifiers)
                {
                    writer.WriteSingle(modifier.Value);
                }
            }
        }

        private void WriteAttributesFull(PooledWriter writer)
        {
            writer.WriteInt32(RuntimeAttributes.Count);

            foreach (SyncRuntimeAttribute runtimeAttribute in RuntimeAttributes)
            {
                writer.WriteString(runtimeAttribute.AttributeType.Id);
                writer.WriteSingle(runtimeAttribute.Value);
            }
        }

        private void WriteClassId(PooledWriter writer)
        {
            _lastSyncedClass = _traitsClass;
            writer.WriteString(_traitsClass.Id);
        }

        public override void Read(PooledReader reader, bool asServer)
        {
            bool writeFull = reader.ReadBoolean();

            if (writeFull)
            {
                ReadClassId(reader, asServer);
                if (!asServer && NetworkManager.IsServer)
                {
                    SkipStatsFull(reader);
                    SkipAttributesFull(reader);
                }
                else
                {
                    ReadStatsFull(reader, asServer);
                    ReadAttributesFull(reader, asServer);
                }
            }
            else
            {
                if (reader.ReadBoolean())
                {
                    ReadClassId(reader, asServer);
                }

                if (!asServer && NetworkManager.IsHost)
                {
                    SkipDelta(reader);
                }
                else
                {
                    ReadDelta(reader, asServer);
                }
            }
        }

        private void ReadDelta(PooledReader reader, bool asServer)
        {
            int changesCount = reader.ReadInt32();

            for (var i = 0; i < changesCount; i++)
            {
                var operation = (SyncTraitsOperation)reader.ReadByte();
                string typeId = reader.ReadString();
                float value = reader.ReadSingle();

                switch (operation)
                {
                    case SyncTraitsOperation.SetStatBase:
                        RuntimeStats.Get(typeId).SetBaseLocally(value, asServer);
                        break;
                    case SyncTraitsOperation.AddConstantModifier:
                        RuntimeStats.Get(typeId).AddModifierLocally(ModifierType.Constant, value, asServer);
                        break;
                    case SyncTraitsOperation.RemoveConstantModifier:
                        RuntimeStats.Get(typeId).RemoveModifierLocally(ModifierType.Constant, value, asServer);
                        break;
                    case SyncTraitsOperation.AddPercentageModifier:
                        RuntimeStats.Get(typeId).AddModifierLocally(ModifierType.Percent, value, asServer);
                        break;
                    case SyncTraitsOperation.RemovePercentageModifier:
                        RuntimeStats.Get(typeId).RemoveModifierLocally(ModifierType.Percent, value, asServer);
                        break;
                    case SyncTraitsOperation.SetAttributeValue:
                        RuntimeAttributes.Get(typeId).SetValueLocally(value, asServer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SkipDelta(PooledReader reader)
        {
            int changesCount = reader.ReadInt32();
            foreach (CachedOnChange change in _clientHostChanges)
            {
                switch (change.Type)
                {
                    case ChangeType.Stat:
                        RuntimeStats.Get(change.TypeId).InvokeClientHostChanged(change.PrevValue, change.Value);
                        break;
                    case ChangeType.Attribute:
                        RuntimeAttributes.Get(change.TypeId).InvokeClientHostChanged(change.PrevValue, change.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            for (var i = 0; i < changesCount; i++)
            {
                reader.ReadByte();
                reader.ReadString();
                reader.ReadSingle();
            }

            _clientHostChanges.Clear();
        }

        private static void SkipStatsFull(PooledReader reader)
        {
            int statsCount = reader.ReadInt32();
            for (var i = 0; i < statsCount; i++)
            {
                reader.ReadString(); // Skip statId
                reader.ReadSingle(); // Skip baseValue
                for (var j = 0; j < reader.ReadInt32(); j++)
                {
                    reader.ReadSingle(); // Skip percent modifier value
                }

                for (var j = 0; j < reader.ReadInt32(); j++)
                {
                    reader.ReadSingle(); // Skip constant modifier value
                }
            }
        }

        private void SkipAttributesFull(PooledReader reader)
        {
            int attributesCount = reader.ReadInt32();
            foreach (CachedOnChange change in _clientHostChanges)
            {
                RuntimeAttributes.Get(change.TypeId).InvokeClientHostChange(change.PrevValue, change.Value);
            }
            for (var i = 0; i < attributesCount; i++)
            {
                reader.ReadString();
                reader.ReadSingle();
            }
        }

        private void ReadStatsFull(PooledReader reader, bool asServer)
        {
            int statsCount = reader.ReadInt32();
            for (var i = 0; i < statsCount; i++)
            {
                string statTypeId = reader.ReadString();
                float baseValue = reader.ReadSingle();
                var modifiers = new Modifiers();
                for (var j = 0; j < reader.ReadInt32(); j++)
                {
                    modifiers.Add(ModifierType.Percent, reader.ReadSingle());
                }
                for (var j = 0; j < reader.ReadInt32(); j++)
                {
                    modifiers.Add(ModifierType.Constant, reader.ReadSingle());
                }

                SyncRuntimeStat runtimeStat = RuntimeStats.Get(statTypeId);
                runtimeStat.CopyDataFrom(baseValue, modifiers);
            }
        }

        private void ReadAttributesFull(PooledReader reader, bool asServer)
        {
            int attributesCount = reader.ReadInt32();
            for (var i = 0; i < attributesCount; i++)
            {
                SyncRuntimeAttribute syncRuntimeAttribute = RuntimeAttributes.Get(reader.ReadString());
                syncRuntimeAttribute.CopyDataFrom(reader.ReadSingle());
            }
        }

        private void ReadClassId(PooledReader reader, bool asServer)
        {
            string traitsClassId = reader.ReadString();
            bool canSetValues = asServer || !NetworkManager.IsServer;
            if (!canSetValues) return;

            Traits.InitializeLocally(traitsClassId);
        }

        internal bool CanNetworkSetValuesInternal(bool warn = true) => CanNetworkSetValues(warn);

        public object GetSerializedType() => null;

        public override void Reset()
        {
            base.Reset();
            _lastSyncedClass = null;
            RuntimeStats.Reset();
            RuntimeAttributes.Reset();
        }
    }
}