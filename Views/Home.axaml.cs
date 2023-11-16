using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Home : UserControl
{
    public Home()
    {
        InitializeComponent();
        LichessAPIUtils.TryGetProfile(profile =>
        {
            SetUserName(profile.username);
            Console.WriteLine(profile.username);
        });

    }
    
    private void SetUserName(string userName)
    {
        UserNameTextBlock.Text = userName;
    }
}