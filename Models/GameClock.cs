using System;
using System.Threading;

namespace LichessClient.Models;

public class GameClock
{
    public bool IsPlayingWhite { get; set; }
    public bool IsPlayerTurn { get; set; }

    private TimeSpan whiteTime;
    private TimeSpan blackTime;

    private Timer timer;
    private readonly int updateInterval = 1000; 

    public Action<string> OnTimePlayer;
    public Action<string> OnTimeOpponent;

    public GameClock()
    {
        timer = new Timer(TimerCallback, null, Timeout.Infinite, updateInterval);
    }

    public void SyncWithServerTime(long newWhiteTimeMs, long newBlackTimeMs, bool isPlayingWhite, bool isPlayerTurn)
    {
        IsPlayingWhite = isPlayingWhite;
        IsPlayerTurn = isPlayerTurn;
        whiteTime = TimeSpan.FromMilliseconds(newWhiteTimeMs);
        blackTime = TimeSpan.FromMilliseconds(newBlackTimeMs);

        timer.Change(0, updateInterval);

        UpdateDisplay();
    }

    private void TimerCallback(object state)
    {
        if (IsPlayerTurn)
        {
            if (IsPlayingWhite)
            {
                whiteTime = DecreaseTime(whiteTime);
            }
            else
            {
                blackTime = DecreaseTime(blackTime);
            }
        }
        else
        {
            if (IsPlayingWhite)
            {
                blackTime = DecreaseTime(blackTime);
            }
            else
            {
                whiteTime = DecreaseTime(whiteTime);
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OnTimePlayer?.Invoke(FormatTime(IsPlayingWhite ? whiteTime : blackTime));
            OnTimeOpponent?.Invoke(FormatTime(IsPlayingWhite ? blackTime : whiteTime));
        });
    }
    
    private TimeSpan DecreaseTime(TimeSpan time)
    {
        return time.TotalSeconds > 0 ? time.Subtract(TimeSpan.FromSeconds(1)) : TimeSpan.Zero;
    }

    private string FormatTime(TimeSpan time)
    {
        return time.ToString(@"hh\:mm\:ss");
    }
    
    public void Dispose()
    {
        timer?.Dispose();
    }
}
