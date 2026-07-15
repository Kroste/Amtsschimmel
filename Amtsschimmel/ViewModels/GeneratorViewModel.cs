using Amtsschimmel.Models;
using Amtsschimmel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amtsschimmel.ViewModels;

/// <summary>Darstellung + Kaufaktionen eines einzelnen Generators.</summary>
public sealed partial class GeneratorViewModel : ObservableObject
{
    private readonly GameEngine _engine;

    public GeneratorDefinition Definition { get; }

    public string Name => Definition.Name;
    public string Description => Definition.Description;

    [ObservableProperty]
    private int _owned;

    [ObservableProperty]
    private string _costText = "";

    [ObservableProperty]
    private string _cost10Text = "";

    [ObservableProperty]
    private string _productionText = "";

    [ObservableProperty]
    private bool _canBuy;

    [ObservableProperty]
    private bool _canBuy10;

    [ObservableProperty]
    private bool _autoBuyerOwned;

    [ObservableProperty]
    private bool _autoBuyerEnabled;

    [ObservableProperty]
    private bool _canBuyAutoBuyer;

    [ObservableProperty]
    private string _autoBuyerCostText = "";

    /// <summary>Erst sichtbar, wenn der Spieler in Reichweite ist (klassisches Unlock-Gefühl).</summary>
    [ObservableProperty]
    private bool _isVisible;

    public GeneratorViewModel(GameEngine engine, GeneratorDefinition definition)
    {
        _engine = engine;
        Definition = definition;
        AutoBuyerCostText = NumberFormatter.Format(definition.AutoBuyerCost);
    }

    /// <summary>Wird vom Haupt-Timer aufgerufen; aktualisiert alle Anzeigewerte.</summary>
    public void Refresh()
    {
        var state = _engine.State.GetGenerator(Definition.Id);
        Owned = state.Owned;
        AutoBuyerOwned = state.AutoBuyerOwned;
        // AutoBuyerEnabled nur setzen, wenn abweichend — verhindert Binding-Ping-Pong.
        if (AutoBuyerEnabled != state.AutoBuyerEnabled)
        {
            AutoBuyerEnabled = state.AutoBuyerEnabled;
        }

        var cost = _engine.NextCost(Definition);
        CostText = NumberFormatter.Format(cost);
        Cost10Text = NumberFormatter.Format(_engine.BulkCost(Definition, 10));
        CanBuy = _engine.CanAfford(cost);
        CanBuy10 = _engine.CanAfford(_engine.BulkCost(Definition, 10));
        CanBuyAutoBuyer = !state.AutoBuyerOwned && _engine.CanAfford(Definition.AutoBuyerCost);
        ProductionText = NumberFormatter.Format(_engine.ProductionPerSecond(Definition)) + "/s";

        // Sichtbar ab: schon gekauft ODER 40 % der Basiskosten erspielt.
        if (!IsVisible && (state.Owned > 0 || _engine.State.TotalEarnedThisRun >= Definition.BaseCost * 0.4))
        {
            IsVisible = true;
        }
    }

    partial void OnAutoBuyerEnabledChanged(bool value)
        => _engine.State.GetGenerator(Definition.Id).AutoBuyerEnabled = value;

    [RelayCommand]
    private void Buy() => _engine.BuyGenerator(Definition);

    [RelayCommand]
    private void Buy10() => _engine.BuyGenerator(Definition, 10);

    [RelayCommand]
    private void BuyAutoBuyer() => _engine.BuyAutoBuyer(Definition);
}
