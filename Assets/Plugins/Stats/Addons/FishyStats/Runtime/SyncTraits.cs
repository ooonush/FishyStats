using System;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using FishNet.Serializing;
using FishNet.Utility.Extension;

namespace Stats.FishNet
{
    internal sealed class SyncTraits : SyncBase, ICustomSync
    {
        private readonly List<Action> _clientHostChanges = new();
        public readonly TraitsSyncChanges Changes;

        public SyncRuntimeStats RuntimeStats => _traits.RuntimeStats;
        public SyncRuntimeAttributes RuntimeAttributes => _traits.RuntimeAttributes;

        private NetworkTraits _traits;

        public SyncTraits()
        {
            Changes = new TraitsSyncChanges(this);
        }

        public void Initialize(NetworkTraits traits)
        {
            _traits = traits;
        }

        public bool UpdateTraitsClass(ITraitsClass traitsClass)
        {
            if (!CanNetworkSetValues()) return false;
            
            Changes.WriteInitializeTraitsClass(traitsClass.Id);
            
            return true;
        }

        public void AddClientHostChange(Action onChangeAction)
        {
            _clientHostChanges.Add(onChangeAction);
        }

        public override void WriteDelta(PooledWriter writer, bool resetSyncTick = true)
        {
            base.WriteDelta(writer, resetSyncTick);
            
            writer.WriteBoolean(false); // False to write delta
            
            Changes.Write(writer);
            Changes.Reset();
        }

        public override void WriteFull(PooledWriter writer)
        {
            WriteHeader(writer, false);
            
            writer.WriteBoolean(true); // write full
            
            writer.WriteString(_traits.TraitsClass.Id);
            
            foreach (IRuntimeStat runtimeStat in RuntimeStats)
            {
                ((ISyncRuntimeStat)runtimeStat).WriteFull(writer);
            }
            foreach (IRuntimeAttribute runtimeAttribute in RuntimeAttributes)
            {
                ((ISyncRuntimeAttribute)runtimeAttribute).WriteFull(writer);
            }
        }

        public override void Read(PooledReader reader, bool asServer)
        {
            bool writeFull = reader.ReadBoolean();
            
            if (writeFull)
            {
                ReadFull(reader, asServer);
            }
            else
            {
                ReadDelta(reader, asServer);
            }
        }

        public void InitializeTraitsClass(string traitsClassId, bool asServer)
        {
            if (NetworkManager.DoubleLogic(asServer)) return;
            
            _traits.InitializeLocally(traitsClassId);
        }

        private void ReadFull(Reader reader, bool asServer)
        {
            string traitsClassId = reader.ReadString();

            if (_traits.IsInitialized)
            {
                if (_traits.TraitsClass.Id != traitsClassId)
                {
                    NetworkManager.LogError("NetworkTraits already initialized with a different TraitsClass.");
                }
            }
            else
            {
                InitializeTraitsClass(traitsClassId, asServer);
            }
            
            foreach (IRuntimeStat runtimeStat in RuntimeStats)
            {
                ((ISyncRuntimeStat)runtimeStat).ReadFull(reader, asServer);
            }
            foreach (IRuntimeAttribute runtimeAttribute in RuntimeAttributes)
            {
                ((ISyncRuntimeAttribute)runtimeAttribute).ReadFull(reader, asServer);
            }
        }

        private void ReadDelta(Reader reader, bool asServer)
        {
            if (NetworkManager.DoubleLogic(asServer))
            {
                foreach (Action action in _clientHostChanges)
                {
                    action.Invoke();
                }
                _clientHostChanges.Clear();
            }
            Changes.ReadAll(reader, asServer);
        }

        internal bool CanNetworkSetValuesInternal(bool warn = true) => CanNetworkSetValues(warn);

        public object GetSerializedType() => null;

        public override void ResetState()
        {
            base.ResetState();
            
            RuntimeStats.Reset();
            RuntimeAttributes.Reset();
        }
    }
}