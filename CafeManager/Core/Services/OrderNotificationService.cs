using System;
using CafeManager.Core.Interfaces;

namespace CafeManager.Core.Services;

public class OrderNotificationService : IOrderNotificationService
{
    public event Action? OnOrdersChanged;

    public void NotifyOrdersChanged()
    {
        if (OnOrdersChanged == null) return;

        foreach (var handler in OnOrdersChanged.GetInvocationList().Cast<Action>())
        {
            try
            {
                handler();
            }
            catch
            {
                // Silence invocation errors for dead circuits
            }
        }
    }
}
