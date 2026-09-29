using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Views;

namespace GymBook.ViewModels;

public partial class HistoryViewModel(DataStore store, StatsService stats, Units units) : BaseViewModel
{
    [ObservableProperty] List<SessionItem> items = [];
    [ObservableProperty] bool isEmpty;
    [ObservableProperty] string summary = "";

    public override Task OnAppearingAsync()
    {
        var history = store.History.ToList();
        Items = history.Select(s => SessionItem.Create(s, store, stats, units)).ToList();
        IsEmpty = Items.Count == 0;
        Summary = $"{history.Count} workouts · {units.FormatVolume(history.Sum(stats.SessionVolume))} lifted";
        return Task.CompletedTask;
    }
}
