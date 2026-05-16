using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using SalesManagementSystem.Data;
using SalesManagementSystem.Models;

namespace SalesManagementSystem.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SalesRepository _repository;
    private Product? _selectedProduct;
    private Customer? _selectedCustomer;
    private SalesOrder? _selectedOrder;
    private Customer? _orderCustomer;
    private Product? _orderProduct;
    private OrderDraftItem? _selectedDraftItem;
    private DashboardSummary _summary = new();
    private int _orderQuantity = 1;
    private string _orderComment = string.Empty;
    private string _statusMessage = "Готово";
    private bool _isBusy;

    public MainViewModel()
        : this(new SalesRepository(AppConfiguration.GetConnectionString()))
    {
    }

    public MainViewModel(SalesRepository repository)
    {
        _repository = repository;
        RefreshCommand = new AsyncRelayCommand(InitializeAsync);
        SaveProductCommand = new AsyncRelayCommand(SaveProductAsync);
        NewProductCommand = new RelayCommand(NewProduct);
        SaveCustomerCommand = new AsyncRelayCommand(SaveCustomerAsync);
        NewCustomerCommand = new RelayCommand(NewCustomer);
        AddOrderItemCommand = new RelayCommand(AddOrderItem);
        RemoveOrderItemCommand = new RelayCommand(RemoveOrderItem);
        CreateOrderCommand = new AsyncRelayCommand(CreateOrderAsync);
    }

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<SalesOrder> Orders { get; } = [];
    public ObservableCollection<OrderDraftItem> DraftItems { get; } = [];
    public ObservableCollection<SalesOrderItem> SelectedOrderItems { get; } = [];

    public DashboardSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public SalesOrder? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                _ = LoadSelectedOrderItemsAsync(value?.Id ?? 0);
            }
        }
    }

    public Customer? OrderCustomer
    {
        get => _orderCustomer;
        set => SetProperty(ref _orderCustomer, value);
    }

    public Product? OrderProduct
    {
        get => _orderProduct;
        set => SetProperty(ref _orderProduct, value);
    }

    public int OrderQuantity
    {
        get => _orderQuantity;
        set => SetProperty(ref _orderQuantity, Math.Max(1, value));
    }

    public string OrderComment
    {
        get => _orderComment;
        set => SetProperty(ref _orderComment, value);
    }

    public OrderDraftItem? SelectedDraftItem
    {
        get => _selectedDraftItem;
        set => SetProperty(ref _selectedDraftItem, value);
    }

    public decimal DraftTotal => DraftItems.Sum(item => item.LineTotal);

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SaveProductCommand { get; }
    public ICommand NewProductCommand { get; }
    public ICommand SaveCustomerCommand { get; }
    public ICommand NewCustomerCommand { get; }
    public ICommand AddOrderItemCommand { get; }
    public ICommand RemoveOrderItemCommand { get; }
    public ICommand CreateOrderCommand { get; }

    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            StatusMessage = "Подключение к SQL Server и подготовка базы...";
            await _repository.InitializeAsync();
            await ReloadAsync();
            StatusMessage = "Данные загружены";
        });
    }

    private async Task ReloadAsync()
    {
        Summary = await _repository.GetDashboardSummaryAsync();
        Replace(Products, await _repository.GetProductsAsync());
        Replace(Customers, await _repository.GetCustomersAsync());
        Replace(Orders, await _repository.GetOrdersAsync());

        SelectedProduct ??= Products.FirstOrDefault();
        SelectedCustomer ??= Customers.FirstOrDefault();
        OrderCustomer ??= Customers.FirstOrDefault();
        OrderProduct ??= Products.FirstOrDefault(product => product.IsActive);
        SelectedOrder ??= Orders.FirstOrDefault();
    }

    private async Task SaveProductAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Создайте или выберите товар.";
            return;
        }

        await RunAsync(async () =>
        {
            var id = await _repository.SaveProductAsync(SelectedProduct);
            StatusMessage = SelectedProduct.Id == 0 ? "Товар добавлен" : "Товар обновлен";
            await ReloadAsync();
            SelectedProduct = Products.FirstOrDefault(product => product.Id == id);
        });
    }

    private void NewProduct()
    {
        SelectedProduct = new Product
        {
            Sku = $"SKU-{DateTime.Now:HHmmss}",
            Category = "Общее",
            IsActive = true
        };
        StatusMessage = "Заполните карточку нового товара";
    }

    private async Task SaveCustomerAsync()
    {
        if (SelectedCustomer is null)
        {
            StatusMessage = "Создайте или выберите клиента.";
            return;
        }

        await RunAsync(async () =>
        {
            var id = await _repository.SaveCustomerAsync(SelectedCustomer);
            StatusMessage = SelectedCustomer.Id == 0 ? "Клиент добавлен" : "Клиент обновлен";
            await ReloadAsync();
            SelectedCustomer = Customers.FirstOrDefault(customer => customer.Id == id);
        });
    }

    private void NewCustomer()
    {
        SelectedCustomer = new Customer();
        StatusMessage = "Заполните карточку нового клиента";
    }

    private void AddOrderItem()
    {
        if (OrderProduct is null)
        {
            StatusMessage = "Выберите товар для позиции заказа.";
            return;
        }

        if (!OrderProduct.IsActive)
        {
            StatusMessage = "Нельзя добавить неактивный товар.";
            return;
        }

        if (OrderQuantity > OrderProduct.StockQuantity)
        {
            StatusMessage = $"На складе доступно только {OrderProduct.StockQuantity} шт.";
            return;
        }

        DraftItems.Add(new OrderDraftItem
        {
            ProductId = OrderProduct.Id,
            ProductName = OrderProduct.Name,
            Quantity = OrderQuantity,
            UnitPrice = OrderProduct.UnitPrice
        });
        OnPropertyChanged(nameof(DraftTotal));
        StatusMessage = "Позиция добавлена в заказ";
    }

    private void RemoveOrderItem()
    {
        if (SelectedDraftItem is null)
        {
            return;
        }

        DraftItems.Remove(SelectedDraftItem);
        SelectedDraftItem = null;
        OnPropertyChanged(nameof(DraftTotal));
        StatusMessage = "Позиция удалена из заказа";
    }

    private async Task CreateOrderAsync()
    {
        if (OrderCustomer is null)
        {
            StatusMessage = "Выберите клиента для заказа.";
            return;
        }

        await RunAsync(async () =>
        {
            var orderId = await _repository.CreateOrderAsync(OrderCustomer.Id, DraftItems, OrderComment);
            DraftItems.Clear();
            OrderComment = string.Empty;
            OnPropertyChanged(nameof(DraftTotal));
            await ReloadAsync();
            SelectedOrder = Orders.FirstOrDefault(order => order.Id == orderId);
            StatusMessage = "Заказ создан, остатки товаров обновлены";
        });
    }

    private async Task LoadSelectedOrderItemsAsync(int orderId)
    {
        SelectedOrderItems.Clear();
        if (orderId == 0)
        {
            return;
        }

        try
        {
            Replace(SelectedOrderItems, await _repository.GetOrderItemsAsync(orderId));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось загрузить состав заказа: {ex.Message}";
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (SqlException ex)
        {
            StatusMessage = $"Ошибка SQL Server: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
