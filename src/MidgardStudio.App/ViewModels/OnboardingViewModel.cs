using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace MidgardStudio.App.ViewModels;

/// <summary>One slide in the first-run onboarding tour.</summary>
public sealed partial class OnboardingPage : ObservableObject
{
    public OnboardingPage(SymbolRegular icon, string title, string body)
    {
        Icon = icon;
        Title = title;
        Body = body;
    }

    public SymbolRegular Icon { get; }
    public string Title { get; }
    public string Body { get; }

    /// <summary>True when this page is the one currently shown (drives the progress dots).</summary>
    [ObservableProperty] private bool _isActive;
}

/// <summary>
/// First-run onboarding: a short, animated feature tour shown exactly once, before the configuration window.
/// "Skip" and the final "Get started" both raise <see cref="Completed"/>, which the shell uses to record that
/// onboarding has been seen and dismiss the overlay.
/// </summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    public OnboardingViewModel()
    {
        Pages = new[]
        {
            new OnboardingPage(SymbolRegular.Sparkle24, "Welcome to Midgard Studio",
                "Edit your Ragnarok server's databases and client files from one native app — no scripts, no guesswork."),
            new OnboardingPage(SymbolRegular.Database24, "Every database, in one place",
                "Items, mobs, pets, skills, combos and groups. Your custom and overridden entries are written safely to the import files — the base data is never touched."),
            new OnboardingPage(SymbolRegular.Image24, "Client items, sprites & GRF",
                "Names, descriptions, icons and worn sprites with live previews, plus a built-in GRF browser — no separate tools to juggle."),
            new OnboardingPage(SymbolRegular.Wand24, "Forge & Autocomplete",
                "Create a complete custom item — server entry, client text and sprite — in a single flow, with rich descriptions generated for you."),
            new OnboardingPage(SymbolRegular.ShieldCheckmark24, "A safety net, built in",
                "A validation gatekeeper catches mistakes before they reach your server, with one-click fixes — and every manual save makes a dated backup."),
        };
        Pages[0].IsActive = true;
    }

    /// <summary>Raised when the user finishes or skips the tour.</summary>
    public event Action? Completed;

    public IReadOnlyList<OnboardingPage> Pages { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    [NotifyPropertyChangedFor(nameof(IsFirst))]
    [NotifyPropertyChangedFor(nameof(IsLast))]
    [NotifyPropertyChangedFor(nameof(StepLabel))]
    private int _currentIndex;

    public OnboardingPage CurrentPage => Pages[CurrentIndex];
    public bool IsFirst => CurrentIndex == 0;
    public bool IsLast => CurrentIndex == Pages.Count - 1;
    public string StepLabel => $"{CurrentIndex + 1} / {Pages.Count}";

    partial void OnCurrentIndexChanged(int oldValue, int newValue)
    {
        if (oldValue >= 0 && oldValue < Pages.Count) Pages[oldValue].IsActive = false;
        if (newValue >= 0 && newValue < Pages.Count) Pages[newValue].IsActive = true;
    }

    [RelayCommand]
    private void Next()
    {
        if (IsLast) Finish();
        else CurrentIndex++;
    }

    [RelayCommand]
    private void Back()
    {
        if (!IsFirst) CurrentIndex--;
    }

    [RelayCommand]
    private void Skip() => Finish();

    [RelayCommand]
    private void Finish() => Completed?.Invoke();
}
