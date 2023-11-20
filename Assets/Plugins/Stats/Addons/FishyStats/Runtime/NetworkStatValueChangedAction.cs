namespace Stats.FishNet
{
    public delegate void NetworkStatValueChangedAction<TNumber>(StatId<TNumber> statId, TNumber prev, TNumber next,
        bool asServer) where TNumber : IStatNumber<TNumber>;
}