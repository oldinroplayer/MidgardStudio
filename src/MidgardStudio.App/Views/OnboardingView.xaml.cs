using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MidgardStudio.App.ViewModels;

namespace MidgardStudio.App.Views;

public partial class OnboardingView : UserControl
{
    private OnboardingViewModel? _vm;

    public OnboardingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => PlayEntrance();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm = e.NewValue as OnboardingViewModel;
        if (_vm is not null) _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The content host is reused across pages, so replay the entrance animation on each page change.
        if (e.PropertyName == nameof(OnboardingViewModel.CurrentIndex)) PlayEntrance();
    }

    private void PlayEntrance()
    {
        if (FindResource("PageEntrance") is Storyboard sb) sb.Begin(this);
    }
}
