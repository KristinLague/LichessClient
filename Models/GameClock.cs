using System;
using System.Threading;

namespace LichessClient.Models;

public class GameClock
{
    private bool m_IsPlayingWhite { get; set; }
    private bool m_IsPlayerTurn { get; set; }

    private TimeSpan m_WhiteTime;
    private TimeSpan m_BlackTime;

    private Timer m_Timer;

    public Action<string> OnTimePlayer;
    public Action<string> OnTimeOpponent;
    
    private const int k_UpdateInterval = 1000; 

    public void Reset()
    {
        m_Timer = new Timer(TimerCallback, null, Timeout.Infinite, k_UpdateInterval);
    }

    public void SyncWithServerTime(long newWhiteTimeMs, long newBlackTimeMs, bool isPlayingWhite, bool isPlayerTurn)
    {
        m_IsPlayingWhite = isPlayingWhite;
        m_IsPlayerTurn = isPlayerTurn;
        m_WhiteTime = TimeSpan.FromMilliseconds(newWhiteTimeMs);
        m_BlackTime = TimeSpan.FromMilliseconds(newBlackTimeMs);

        m_Timer?.Change(0, k_UpdateInterval);

        UpdateDisplay();
    }

    private void TimerCallback(object state)
    {
        if (m_IsPlayerTurn)
        {
            if (m_IsPlayingWhite)
            {
                m_WhiteTime = DecreaseTime(m_WhiteTime);
            }
            else
            {
                m_BlackTime = DecreaseTime(m_BlackTime);
            }
        }
        else
        {
            if (m_IsPlayingWhite)
            {
                m_BlackTime = DecreaseTime(m_BlackTime);
            }
            else
            {
                m_WhiteTime = DecreaseTime(m_WhiteTime);
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OnTimePlayer?.Invoke(FormatTime(m_IsPlayingWhite ? m_WhiteTime : m_BlackTime));
            OnTimeOpponent?.Invoke(FormatTime(m_IsPlayingWhite ? m_BlackTime : m_WhiteTime));
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
        m_Timer?.Dispose();
    }
}
