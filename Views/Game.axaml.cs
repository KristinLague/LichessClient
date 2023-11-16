using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Game : UserControl
{
    public Game()
    {
        InitializeComponent();
        ActiveBoard.OnGameStarted(AppController.Instance.lastGameFull);
    }
}