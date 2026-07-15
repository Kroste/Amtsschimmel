using Amtsschimmel.Models;
using Amtsschimmel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amtsschimmel.ViewModels;

/// <summary>Eine Fortbildung der Verwaltungsakademie im Forschungs-Tab.</summary>
public sealed partial class ResearchViewModel : ObservableObject
{
    private readonly GameEngine _engine;

    public ResearchDefinition Definition { get; }

    public string Name => Definition.Name;
    public string Description => Definition.Description;
    public string EffectText => Definition.EffectText;
    public string CostText { get; }

    /// <summary>Namen der Voraussetzungen, z. B. "Benötigt: Gleitzeitmodell".</summary>
    public string PrerequisiteText { get; }
    public bool HasPrerequisites { get; }

    [ObservableProperty]
    private bool _isResearched;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _canResearch;

    public ResearchViewModel(GameEngine engine, ResearchDefinition definition)
    {
        _engine = engine;
        Definition = definition;
        CostText = NumberFormatter.Format(definition.Cost);
        var prereqNames = (definition.Prerequisites ?? [])
            .Select(id => ResearchDefinitions.All.FirstOrDefault(r => r.Id == id)?.Name ?? id)
            .ToArray();
        HasPrerequisites = prereqNames.Length > 0;
        PrerequisiteText = HasPrerequisites ? "Benötigt: " + string.Join(", ", prereqNames) : "";
    }

    /// <summary>Wird vom Haupt-Timer aufgerufen; aktualisiert alle Anzeigewerte.</summary>
    public void Refresh()
    {
        IsResearched = _engine.IsResearched(Definition);
        IsLocked = !IsResearched && !_engine.PrerequisitesMet(Definition);
        CanResearch = _engine.CanResearch(Definition);
    }

    [RelayCommand]
    private void Buy() => _engine.BuyResearch(Definition);
}
