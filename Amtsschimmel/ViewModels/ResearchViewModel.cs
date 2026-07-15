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

    /// <summary>Namen der Voraussetzungen, z. B. "Benötigt: Gleitzeitmodell".</summary>
    public string PrerequisiteText { get; }
    public bool HasPrerequisites { get; }

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private string _levelText = "";

    [ObservableProperty]
    private string _costText = "";

    [ObservableProperty]
    private bool _isMaxed;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _canResearch;

    public ResearchViewModel(GameEngine engine, ResearchDefinition definition)
    {
        _engine = engine;
        Definition = definition;
        var requirements = (definition.Prerequisites ?? [])
            .Select(id => ResearchDefinitions.All.FirstOrDefault(r => r.Id == id)?.Name ?? id)
            .ToList();
        if (definition.MinReformen > 0)
        {
            requirements.Add($"Verwaltungsreform Nr. {definition.MinReformen}");
        }
        HasPrerequisites = requirements.Count > 0;
        PrerequisiteText = HasPrerequisites ? "Benötigt: " + string.Join(", ", requirements) : "";
    }

    /// <summary>Wird vom Haupt-Timer aufgerufen; aktualisiert alle Anzeigewerte.</summary>
    public void Refresh()
    {
        Level = _engine.ResearchLevel(Definition);
        IsMaxed = _engine.IsMaxed(Definition);
        IsLocked = Level == 0
            && (!_engine.PrerequisitesMet(Definition) || !_engine.ReformRequirementMet(Definition.MinReformen));
        CanResearch = _engine.CanResearch(Definition);
        CostText = IsMaxed ? "" : NumberFormatter.Format(_engine.NextResearchCost(Definition));
        LevelText = Definition switch
        {
            { IsEndless: true } => $"Stufe {Level} / ∞",
            { IsRepeatable: true } => $"Stufe {Level} / {Definition.MaxLevel}",
            _ => Level >= 1 ? "Abgeschlossen" : "",
        };
    }

    [RelayCommand]
    private void Buy() => _engine.BuyResearch(Definition);
}
