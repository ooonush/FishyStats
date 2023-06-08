namespace Stats.FishNet
{
    public delegate void NetworkStatValueChangedAction(StatType statType, float prev, float next, bool asServer);
}