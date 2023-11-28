using FishNet.Serializing;

namespace Stats.FishNet
{
    internal interface ISyncRuntimeStat : IRuntimeStat
    {
        void WriteFull(Writer writer);
        void ReadFull(Reader reader, bool asServer);
        void ReadAddConstantModifier(Reader reader, ModifierType modifierType, bool asServer);
        void ReadRemoveConstantModifier(Reader reader, ModifierType modifierType, bool asServer);
        void ReadAddPercentageModifier(Reader reader, ModifierType modifierType, bool asServer);
        void ReadRemovePercentageModifier(Reader reader, ModifierType modifierType, bool asServer);
        void ReadSetStatBaseOperation(Reader reader, bool asServer);
    }
}