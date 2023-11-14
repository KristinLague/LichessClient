using System;
using Avalonia.Controls;

namespace LichessClient.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var loginButton = this.FindControl<Button>("LoginButton");
        if (loginButton != null)
        {
            loginButton.Click += LoginButton_Click;
        }
    }

    private void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!Program.HasToken())
        {
            Program.StartAuthentication();
        }
        else
        {
            Console.WriteLine("Already logged in!");
        }
    }
}