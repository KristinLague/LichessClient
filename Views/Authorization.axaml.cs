using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Authorization : UserControl
{
    public Authorization()
    {
        InitializeComponent();
        Setup();
    }
    private void Setup()
    {
        var loginButton = this.FindControl<Button>("LoginButton");
        if (loginButton != null)
        {
            loginButton.Click += LoginButton_Click;
        }
    }
    
    private void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppController.Instance.StartAuthentication();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        var loginButton = this.FindControl<Button>("LoginButton");
        if (loginButton != null)
        {
            loginButton.Click -= LoginButton_Click;
        }
    }
}