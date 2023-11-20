namespace Stats.FishNet
{
    public enum SyncTraitsOperation : byte
    {
        SetStatBase,
        AddConstantNegativeModifier,
        RemoveConstantNegativeModifier,
        AddPercentageNegativeModifier,
        RemovePercentageNegativeModifier,
        AddConstantPositiveModifier,
        RemoveConstantPositiveModifier,
        AddPercentagePositiveModifier,
        RemovePercentagePositiveModifier,
        SetAttributeValue,
        InitializeTraitsClass
    }
}