# Scandish - Cafe Management System

Scandish is a modern, responsive cafe management application designed for seamless customer ordering and efficient kitchen operations.

## 🛠 Technical Stack
- **Framework**: Blazor Server (ASP.NET Core 8)
- **Styling**: Tailwind CSS with custom "Cafe Espresso" design system
- **Database**: Entity Framework Core (PostgreSQL for production, SQLite for local fallback)
- **Real-time**: SignalR for instant order updates and kitchen notifications
- **Storage**: Local browser storage for cart persistence and order history
- **Image Handling**: Base64 encoding for embedded menu item images

## 🚀 Key Features
- **Smart Menu**: Categorized menu with popular item highlights and tag-based search.
- **Dynamic Ordering**: Supports item variants (sizes) and customizable add-ons.
- **Live Order Tracking**: Real-time status bar for customers (Pending → Cooking → Ready).
- **Kitchen Kanban**: Efficient order management for staff with auditory notifications.
- **Admin Tools**: Comprehensive dashboard for menu, category, and outlet management.

## 🏗 Implementation Step-by-Step

### 1. Foundation & Database
- Initialized the ASP.NET Core Blazor Server project.
- Configured a dual-database strategy using **PostgreSQL** with a seamless fallback to **SQLite** for development flexibility.
- Defined core entities: `MenuItem`, `Category`, `Order`, `OrderItem`, and `Addon`.

### 2. Modern Design System
- Built a custom UI library in `Shared/UI` with reusable components like `ScandishModal`, `ScandishInput`, and `ScandishButton`.
- Applied a premium "Scandish" aesthetic using a curated palette (Espresso, Mocha, Caramel, Cream).

### 3. Customer Menu & Cart
- Developed a responsive menu with horizontal category scrolling.
- Implemented an intelligent cart system that merges identical items (matching variants and addons) to keep the order clean.
- Created the `ItemDetailsModal` with sticky headers/footers for a smooth mobile experience.

### 4. Real-time Kitchen Queue
- Built a Kanban-style dashboard for staff to manage orders.
- Integrated **SignalR** to push updates to both staff and customers instantly.
- Added sound alerts for incoming orders to ensure kitchen staff never miss a new ticket.

### 5. Management & Optimization
- Created admin interfaces for full control over menu items and categories.
- Optimized performance using `AsNoTracking` for read-only queries and Eager Loading (`Include`) for complex data relations.
- Fixed UI constraints like modal scrollability and responsive layout breaks to ensure production readiness.

## 💡 Developer Insights & Architecture Details

As a developer (especially if you're coming from a Python background like **Flask/Django** or **FastAPI**), here are some specific implementation details that make this project tick:

### 1. The Real-time Engine (SignalR)
- **What it is**: Think of this as a managed WebSockets wrapper. 
- **Implementation**: We use `IOrderNotificationService` to broadcast events. When an order status changes in the database, a SignalR message is pushed to all connected clients.
- **Python Comparison**: Similar to using **Channels** in Django or **WebSockets** with FastAPI.

### 2. Audio & JS Interop
- **The Sound**: We use a clean "Notification" sound for the kitchen. 
- **How it works**: Since Blazor runs on the server, it can't directly play sounds in the user's browser. We use **JSInterop** (`IJSRuntime`) to call a small JavaScript function in `wwwroot/js/audio.js` which triggers the `Audio` object.
- **Developer Tip**: Browsers block auto-playing audio unless the user has interacted with the page first. We handle this by ensuring the first user click enables the audio context.

### 3. Data Patterns (Soft Deletes & Eager Loading)
- **Soft Deletes**: Instead of `DELETE`, we use an `IsDeleted` flag in the database. This is a production best practice to prevent accidental data loss.
- **Eager Loading**: To avoid the "N+1 Problem" (making 100 database calls for 100 items), we use `.Include(x => x.Category)` in our EF Core queries to fetch related data in a single SQL Join.

### 4. Circuit Stability (The Blazor "Gotcha")
- Blazor Server maintains a persistent connection (Circuit). If an unhandled exception occurs in an `async void` method, the entire UI crashes.
- **Fix**: We wrapped event handlers in robust Try-Catch blocks and used `InvokeAsync(StateHasChanged)` to ensure the UI updates safely from background threads.

### 5. Simple Image Storage
- For this project, we avoid the complexity of an S3 bucket. Images are resized on the client-side using a `Canvas` API, converted to **Base64 strings**, and stored directly in the database. This makes the project entirely self-contained and easy to move between environments.

## 🏃 How to Run
1. **Prerequisites**: .NET 8 SDK installed.
2. **Launch**: Run `dotnet run --project CafeManager.csproj`
3. **Explore**: 
   - **Customer Menu**: `http://localhost:5000/menu`
   - **Kitchen Queue**: `http://localhost:5000/queue`
   - **Kitchen Dashboard**: `http://localhost:5000/kitchen`
   - **Admin Panel**: `http://localhost:5000/manage`
