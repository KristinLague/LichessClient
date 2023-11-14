using System;
using Avalonia.Controls;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();
        Setup();
    }

    private void Setup()
    {
        AppController.Instance.OnAppStateChanged += OnAppStateChanged;
        OnAppStateChanged(this, AppController.Instance.CurrenAppState);
    }

    private void OnAppStateChanged(object? sender, AppStates updatedState)
    {
        Content = updatedState switch
        {
            AppStates.Authorization => new Authorization(),
            AppStates.Home => new Home(),
            _ => throw new ArgumentOutOfRangeException(nameof(updatedState), updatedState, null)
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        AppController.Instance.OnAppStateChanged -= OnAppStateChanged;
    }
    
}