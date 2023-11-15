using System;
using Avalonia.Controls;
using LichessClient.Models;

namespace LichessClient.Views;

public partial class Home : UserControl
{
    public Home()
    {
        InitializeComponent();
        GetProfile();
    }

    private async void GetProfile()
    {
        try
        {
            Profile? profile = await LichessAPIUtils.TryGetProfile();
            if (profile != null)
            {
                //DOUI
            }
            else
            {
                //Throw Error
            }
        }
        catch (Exception e)
        {
            //TODO: Add toast for errors like this
            Console.WriteLine($"Exception: {e.Message}");
        }
    }
}