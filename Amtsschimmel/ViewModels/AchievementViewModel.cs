using Amtsschimmel.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Amtsschimmel.ViewModels;

/// <summary>Ein Achievement in der Erfolgsliste — versteckt Details bis zur Freischaltung.</summary>
public sealed partial class AchievementViewModel(AchievementDefinition definition) : ObservableObject
{
    public AchievementDefinition Definition { get; } = definition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(DisplayDescription))]
    private bool _isUnlocked;

    public string DisplayName => IsUnlocked ? Definition.Name : "???";
    public string DisplayDescription => IsUnlocked ? Definition.Description : "Noch nicht freigeschaltet.";
}
