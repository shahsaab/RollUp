using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CafeManager.Core.Enums;
using CafeManager.Core.Interfaces;
using CafeManager.Core.Models;

namespace CafeManager.Core.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<CafeManager.Core.Entities.Order> _orderRepository;
    private readonly IRepository<CafeManager.Core.Entities.OrderItem> _orderItemRepository;
    private readonly IRepository<CafeManager.Core.Entities.MenuItem> _menuItemRepository;
    private readonly IRepository<CafeManager.Core.Entities.Outlet> _outletRepository;
    private readonly IOrderNotificationService _notificationService;

    public event Action? OnOrdersChanged
    {
        add => _notificationService.OnOrdersChanged += value;
        remove => _notificationService.OnOrdersChanged -= value;
    }

    public OrderService(
        IRepository<CafeManager.Core.Entities.Order> orderRepository,
        IRepository<CafeManager.Core.Entities.OrderItem> orderItemRepository,
        IRepository<CafeManager.Core.Entities.MenuItem> menuItemRepository,
        IRepository<CafeManager.Core.Entities.Outlet> outletRepository,
        IOrderNotificationService notificationService)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _menuItemRepository = menuItemRepository;
        _outletRepository = outletRepository;
        _notificationService = notificationService;
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllWithIncludeAsync(o => o.Items);
        var allMenuItems = await _menuItemRepository.GetAllAsync();

        return orders.OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToModel((CafeManager.Core.Entities.Order)o, ((CafeManager.Core.Entities.Order)o).Items, allMenuItems))
            .ToList();
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var orders = await _orderRepository.FindWithIncludeAsync(o => o.Status == status, o => o.Items);
        var allMenuItems = await _menuItemRepository.GetAllAsync();

        return orders.OrderBy(o => o.CreatedAt)
            .Select(o => MapToModel((CafeManager.Core.Entities.Order)o, ((CafeManager.Core.Entities.Order)o).Items, allMenuItems))
            .ToList();
    }

    public async Task<List<Order>> GetActiveOrdersAsync()
    {
        var orders = await _orderRepository.FindWithIncludeAsync(
            o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled,
            o => o.Items);
        var allMenuItems = await _menuItemRepository.GetAllAsync();

        return orders.OrderBy(o => o.CreatedAt)
            .Select(o => MapToModel((CafeManager.Core.Entities.Order)o, ((CafeManager.Core.Entities.Order)o).Items, allMenuItems))
            .ToList();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var entity = await _orderRepository.GetByIdAsync(id);
        if (entity == null) return null;

        var items = await _orderItemRepository.FindAsync(i => i.OrderId == id);
        var menuItems = await _menuItemRepository.GetAllAsync();
        return MapToModel(entity, items, menuItems);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        var orders = await _orderRepository.FindAsync(o => o.OrderNumber == orderNumber);
        var entity = orders.FirstOrDefault();
        if (entity == null) return null;

        var items = await _orderItemRepository.FindAsync(i => i.OrderId == entity.Id);
        var menuItems = await _menuItemRepository.GetAllAsync();
        return MapToModel(entity, items, menuItems);
    }

    public async Task<Order> CreateOrderAsync(string customerName, List<CartItem> items, string tableNumber, OrderType type)
    {
        // Use a more robust numbering system: #YYMMDD-XXXX (where XXXX is the ID after first save)
        // Or for now, just # + ticks to ensure uniqueness without double save
        var orderNumber = $"#{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

        var outlets = await _outletRepository.GetAllAsync();
        var defaultOutletId = outlets.FirstOrDefault()?.Id ?? 1;

        var orderEntity = new CafeManager.Core.Entities.Order
        {
            OrderNumber = orderNumber,
            CustomerName = customerName,
            TableNumber = tableNumber,
            Type = type,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OutletId = defaultOutletId
        };

        // Add all items to the order entity first
        foreach (var item in items)
        {
            orderEntity.Items.Add(new CafeManager.Core.Entities.OrderItem
            {
                MenuItemId = item.MenuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = item.MenuItem.Price,
                SelectedVariant = item.SelectedVariant,
                SelectedAddons = string.Join(",", item.SelectedAddons),
                SpecialInstructions = item.SpecialInstructions
            });
        }

        // Save everything in one go
        await _orderRepository.AddAsync(orderEntity);
        await _orderRepository.SaveChangesAsync();

        var allMenuItems = await _menuItemRepository.GetAllAsync();
        var result = MapToModel(orderEntity, orderEntity.Items, allMenuItems);
        
        _notificationService.NotifyOrdersChanged();
        return result;
    }

    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var entity = await _orderRepository.GetByIdAsync(orderId);
        if (entity != null)
        {
            entity.Status = newStatus;
            if (newStatus == OrderStatus.Completed)
            {
                entity.CompletedAt = DateTime.UtcNow;
            }
            _orderRepository.Update(entity);
            await _orderRepository.SaveChangesAsync();
            _notificationService.NotifyOrdersChanged();
        }
    }

    public async Task CancelOrderAsync(int orderId)
    {
        await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
    }

    public async Task<List<Order>> GetOrdersByNumbersAsync(IEnumerable<string> orderNumbers)
    {
        var orders = await _orderRepository.FindWithIncludeAsync(
            o => orderNumbers.Contains(o.OrderNumber),
            o => o.Items);
        var allMenuItems = await _menuItemRepository.GetAllAsync();

        return orders.OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToModel((CafeManager.Core.Entities.Order)o, ((CafeManager.Core.Entities.Order)o).Items, allMenuItems))
            .ToList();
    }

    private Order MapToModel(
        CafeManager.Core.Entities.Order entity, 
        IEnumerable<CafeManager.Core.Entities.OrderItem> items,
        IEnumerable<CafeManager.Core.Entities.MenuItem> menuItems)
    {
        return new Order
        {
            Id = entity.Id,
            OrderNumber = entity.OrderNumber,
            CustomerName = entity.CustomerName,
            TableNumber = entity.TableNumber,
            Status = entity.Status,
            Type = entity.Type,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt,
            Items = items.Select(i => new CartItem
            {
                Quantity = i.Quantity,
                SelectedVariant = i.SelectedVariant,
                SelectedAddons = (i.SelectedAddons ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                SpecialInstructions = i.SpecialInstructions,
                MenuItem = MapMenuItem(menuItems.FirstOrDefault(m => m.Id == i.MenuItemId))
            }).ToList()
        };
    }

    private MenuItem MapMenuItem(CafeManager.Core.Entities.MenuItem? entity)
    {
        if (entity == null) return new MenuItem { Name = "Unknown Item" };
        return new MenuItem
        {
            Id = entity.Id,
            Name = entity.Name,
            Price = entity.Price,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            IsAvailable = entity.IsAvailable,
            IsPopular = entity.IsPopular,
            Tags = (entity.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }
}
