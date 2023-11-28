using FishNet.Serializing;

namespace Stats.FishNet
{
    internal interface ISyncRuntimeAttribute : IRuntimeAttribute
    {
        void WriteFull(Writer writer);
        void ReadFull(Reader reader, bool asServer);
        void ReadSetValueOperation(Reader reader, bool asServer);
    }
}