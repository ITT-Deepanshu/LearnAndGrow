public class OrderProcessor
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;

    public OrderProcessor(
        IPaymentGateway paymentGateway,
        IInventoryService inventoryService,
        INotificationService notificationService)
    {
        _paymentGateway = paymentGateway;
        _inventoryService = inventoryService;
        _notificationService = notificationService;
    }

    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        ValidateOrderNotNull(order);

        if (!IsOrderEligibleForProcessing(order))
        {
            return OrderResult.Invalid("Order validation failed");
        }

        if (!await HasSufficientInventoryAsync(order))
        {
            return OrderResult.Failed("Insufficient inventory");
        }

        await _inventoryService.ReserveItems(order.Items);

        try
        {
            return await CompletePaymentAndFinalizeOrderAsync(order);
        }
        catch
        {
            await _inventoryService.ReleaseReservation(order.Items);
            throw;
        }
    }

    private static void ValidateOrderNotNull(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
    }

    private static bool IsOrderEligibleForProcessing(Order order)
    {
        return order.Items?.Any() == true && order.TotalAmount > 0;
    }

    private async Task<bool> HasSufficientInventoryAsync(Order order)
    {
        return await _inventoryService.CheckAvailability(order.Items);
    }

    private async Task<OrderResult> CompletePaymentAndFinalizeOrderAsync(Order order)
    {
        var paymentResult = await _paymentGateway.ProcessPayment(
            order.CustomerId,
            order.TotalAmount,
            order.PaymentMethod);

        if (!paymentResult.IsSuccessful)
        {
            await _inventoryService.ReleaseReservation(order.Items);
            return OrderResult.Failed($"Payment failed: {paymentResult.ErrorMessage}");
        }

        await _inventoryService.CommitReservation(order.Items);
        await _notificationService.SendOrderConfirmation(order);

        return OrderResult.Success(paymentResult.TransactionId);
    }

    public async Task CancelOrderAsync(string orderId)
    {
        var order = await GetOrderByIdAsync(orderId);

        if (order.Status == OrderStatus.Paid)
        {
            await RefundAndRestoreInventoryAsync(order);
        }

        order.Status = OrderStatus.Cancelled;
        await SaveOrderAsync(order);
    }

    private async Task RefundAndRestoreInventoryAsync(Order order)
    {
        await _paymentGateway.RefundPayment(order.TransactionId);
        await _inventoryService.RestoreInventory(order.Items);
    }

    private async Task<Order> GetOrderByIdAsync(string orderId)
    {
        return await Task.FromResult(new Order());
    }

    private async Task SaveOrderAsync(Order order)
    {
        await Task.CompletedTask;
    }
}
