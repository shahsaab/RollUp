using System;

namespace CafeManager.Core.Interfaces;

public interface IOrderNotificationService
{
    event Action OnOrdersChanged;
    void NotifyOrdersChanged();
}
