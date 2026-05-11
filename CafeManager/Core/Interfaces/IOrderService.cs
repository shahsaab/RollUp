using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CafeManager.Core.Enums;
using CafeManager.Core.Models;

namespace CafeManager.Core.Interfaces;

public interface IOrderService
{
    event Action? OnOrdersChanged;
    Task<List<Order>> GetAllOrdersAsync();
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);
    Task<List<Order>> GetActiveOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<Order> CreateOrderAsync(string customerName, List<CartItem> items, string tableNumber, OrderType type);
    Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    Task CancelOrderAsync(int orderId);
    Task<List<Order>> GetOrdersByNumbersAsync(IEnumerable<string> orderNumbers);
}
