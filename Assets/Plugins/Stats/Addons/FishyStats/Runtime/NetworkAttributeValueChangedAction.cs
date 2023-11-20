namespace Stats.FishNet
{
    public delegate void NetworkAttributeValueChangedAction<TNumber>(AttributeId<TNumber> attributeId, TNumber prev, TNumber next, bool asServer) where TNumber : IStatNumber<TNumber>;
}