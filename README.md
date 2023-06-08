# FishyStats

_This asset is an **addon** to the [**Stats**](https://github.com/ooonush/Stats) asset. Please familiarize yourself with it before working with **FishyStats**._

Most games include a stat system, whether it's a simple health scale with a fixed value or a more complex RPG system with the ability to pump and many interconnected stats that are calculated over the course of the game.
The purpose of the Stats assets is to make it easier for Unity developers to add, maintain, and extend this kind of functionality to their games.

_This version is in **preview** stage and may be unstable, it is **not recommended to use it in production**. If you encounter any bugs, please make a bug report. You can also suggest features that you would like to see in the Asset._

## Installation

Requires a version of **Unity 2021.3** or newer.

Make sure you have the following packages installed:
1. **[Stats](https://github.com/ooonush/Stats)**
2. **[Fish-Net](https://github.com/FirstGearGames/FishNet)**

To install **FishyStats** you can add `https://github.com/ooonush/FishyStats.git?path=Assets/Plugins/FishyStats` to **Package Manager**

![image](https://github.com/ooonush/FishyStats/assets/72870405/5a36463a-354f-481a-b4a8-dd7912d82694)

![image](https://github.com/ooonush/FishyStats/assets/72870405/c20b0884-953c-4d67-822d-77dff441202e)

If you want to set a target version, **FishyStats** uses the *.*.* release tag so you can specify a version like #0.0.1-pre.1. For example `https://github.com/ooonush/FishyStats.git?path=Assets/Plugins/FishyStats#0.0.1-pre.1`.

## The main differences from Stats

### Editor

All stats configuration is exactly the same, but for stats to be synchronized over the network, you must use the **NetworkTraits** component instead of **Traits**.

![image](https://github.com/ooonush/FishyStats/assets/72870405/d8b562aa-b895-4a0a-80b3-454cd17d06ec)

Unlike **Traits**, in NetworkTraits you must specify **TraitsClassRegistry**. In this object you must specify all **Traits Classes** that can be used by this **NetworkTraits** component.

You can also have a single **TraitsClassRegistry** object that contains _all_ the **Traits Classes** in your game.

To create a **TraitsClassRegistry**, right click on the project window and go to **Create → Stats → TraitsClassRegistry**.

For example, if the game has 3 traits classes that will be synchronized over the network, you must specify all 3 in the **TraitsClassRegistry**.

![image](https://github.com/ooonush/FishyStats/assets/72870405/1371385d-a059-4061-9432-bcf9e4f71637)

_This object must be the same on all clients, otherwise synchronization may not work._

### Scripting

I tried to make it so there are no differences, so the **[scripting guide](https://github.com/ooonush/Stats)** for **Traits** will work just fine for **NetworkTraits**.

The main thing to remember is that:
1) It is best to call **Initialize()** before the object is _spawned_ in network.
2) Access **RuntimeStats** and **RuntimeAttributes** during and after calling **OnNetworkStart()**.

#### A simple example of a NetworkCharacter class

```csharp
public class NetworkCharacter : NetworkBehaviour
{
    [SerializeField] private NetworkTraits _networkTraits;

    [SerializeField] private StatType _strengthType;
    [SerializeField] private StatType _maxHealthType;
    [SerializeField] private AttributeType _healthType;

    private SyncRuntimeStat _strength;
    private SyncRuntimeStat _maxHealth;
    private SyncRuntimeAttribute _health;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _strength = _networkTraits.RuntimeStats.Get(_strengthType);
        _maxHealth = _networkTraits.RuntimeStats.Get(_maxHealthType);
        _health = _networkTraits.RuntimeAttributes.Get(_healthType);
    }

    public void ReceiveDamage(int damage)
    {
        _health.Value -= damage;
    }

    public void PutArmor(int protection)
    {
        _maxHealth.AddModifier(ModifierType.Constant, protection);
    }

    public void RemoveArmor(int protection)
    {
        _maxHealth.RemoveModifier(ModifierType.Constant, protection);
    }
}
```
