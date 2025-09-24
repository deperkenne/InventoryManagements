using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Repositories.Impl;
using BestandsManager.Logistics.Repository;


/**
// create some orders with order lines
Order order001 = new Order("release");
order001.AddOrderLine(new OrderLine(new ElectronicProduct("125", "RADIO", 30)));
order001.AddOrderLine(new OrderLine(new ElectronicProduct("126", "TELEVISION", 50)));

// change product name in order line
order001.OrderLines[0].Product.SetName("NEW RADIO");

// create another order with order lines
Order order002 = new Order("in progress", true, OrderPriority.High);
order002.AddOrderLine(new OrderLine(new ElectronicProduct("127b", "TEL", 30)));
order002.AddOrderLine(new OrderLine(new ElectronicProduct("125", "RADIO", 30)));

// create and save order in Memory
IOrderRepository orderRepository = new InMemoryOrderRepository();
orderRepository.Save(order001);
orderRepository.Save(order002);

// print all orders in the repository
Console.WriteLine(orderRepository.GetAll());



// show details of each order
foreach (Order o in orderRepository.GetAll())
{
    o.printOrderDetail();
}



// Initialize and display orders from InMemoryOrderRepository

Console.WriteLine("Memory containt Before update..............................");
IOrderRepository InMemoryOrderRepository = new InMemoryOrderRepository();
InMemoryOrderRepository.InitializeOrders();
InMemoryOrderRepository.DisplayOrders();



// update order status example
Console.WriteLine("Memory containt After update..............................");
InMemoryOrderRepository.Update(InMemoryOrderRepository.GetAll().First().Key,"Delivry");
InMemoryOrderRepository.DisplayOrders();


// Add new order example
Console.WriteLine("Memory containt After Add new Order..............................");
Order newOrder = new Order("New Order", false, OrderPriority.Low);
newOrder.AddOrderLine(new OrderLine(new Saft("129", "COLA 1L", 20)));
newOrder.AddOrderLine(new OrderLine(new Saft("130", "FANTA 1L", 20)));


InMemoryOrderRepository.Save(newOrder);
InMemoryOrderRepository.DisplayOrders();


// Get All orders with status "Release"

Console.WriteLine("Get List commande with status release.............................."+"\n\n\n");
foreach (Order order in InMemoryOrderRepository.GetByStatus("Release"))
{
    order.printOrderDetail();
}
;

// delete all orders with status "Delivry"
Console.WriteLine("Get List commande without Deliry status .............................." + "\n\n\n");
InMemoryOrderRepository.Delete("Delivry");
InMemoryOrderRepository.DisplayOrders();







IwarehouseRepository warehouseRepository = new Warehouse();
warehouseRepository.InitializeWarehouse();

InMemoryOrderRepository inMemoryOrderRepository = new InMemoryOrderRepository();
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
inMemoryOrderRepository.InitializeOrders();
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

//warehouseRepository.GetAllWarehouseData();


StockService stockService = new StockService(inMemoryOrderRepository.GetAllAsListAsync(),warehouseRepository.GetAll());


var sortedOrdersTask = stockService.SortOrdersByPriorityAndDateAsync("release");
sortedOrdersTask.Wait(); // Alternativ: await in einer async-Methode verwenden
foreach (var order in sortedOrdersTask.Result)
{
    order.printOrderDetail();
}




InMemoryOrderRepository inMemoryOrderRepository = new InMemoryOrderRepository();

OrderService orderService = new OrderService(inMemoryOrderRepository);
var ordersTask = orderService.GetAllOrders(); // fecht orders asynchronously
foreach (var order in await ordersTask)
{
    order.printOrderDetail();
}



OrderServiceImpl orderServiceImpl = new OrderServiceImpl();
orderServiceImpl.InitializeOrders();

AllocationService allocationService = new AllocationService(orderServiceImpl, new StockServiceImpl());


var newOrdersTask = allocationService.GetNewOrdersAsync(EnumOrderStatus.NEW); // fecht new orders asynchronously

foreach (var order in await newOrdersTask)
{
    order.printOrderDetail();
        Console.WriteLine(order.GetEnumStatus());
}





IStockRepository stockRepository = new StockServiceImpl();

StockServiceImpl stockServiceImpl = new StockServiceImpl();

Console.WriteLine(stockServiceImpl.skuInMemory);

stockServiceImpl.calculateAvailableQuantity("ORANGE_01").Wait();
foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.productName} , isloked: {stcok.isLocked}, alocatedqtey: {stcok.GetAllocatedQuantity}, removed: {stcok.removedQuantity}");
}


var stockTask = stockRepository.GetByProductName("ORANGE_01"); // fecht stock asynchronously

**/


InMemoryOrderImpl orderServiceImpl = new InMemoryOrderImpl();
InMemoryStockImpl stockServiceImpl = new InMemoryStockImpl();
IEventStoreRepository eventStoreRepository = new InMemoryEventStoreImpl();

stockServiceImpl.GetAvailableSkusAsync("ORANGE_L1").Wait();


Console.WriteLine("List of all orders after allocation process.............................." + "\n\n\n");


OrderCancellationService orderCancellationService = new OrderCancellationService(eventStoreRepository, orderServiceImpl, stockServiceImpl);
OrderAllocationServiceImpl allocationService = new OrderAllocationServiceImpl(orderServiceImpl, stockServiceImpl, eventStoreRepository, orderCancellationService);
AdjustSkuQuantityManuellyService adjustSkuQuantityService = new AdjustSkuQuantityManuellyService(stockServiceImpl, orderServiceImpl, eventStoreRepository, allocationService);

Console.WriteLine("List of all sku table before automatic  and Manuelly process.............................." + "\n\n\n");
foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");
    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName}  , isloked:  {stcok.GetIsLocked} , alocatedqtey:  {stcok.GetAllocatedQuantity()}");
}

await allocationService.ProcessMultipleOrdersForAllocationAsync(); // start automatic process

var allEventStore = await allocationService.GetAllEventAsync();


Console.WriteLine("List of all sku table after automatic process.............................." + "\n\n\n");

foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");
    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName} , isloked: {stcok.GetIsLocked}, alocatedqtey: {stcok.GetAllocatedQuantity()}");
}


/*

Console.WriteLine("status eventstore.............................." + "\n\n\n");
foreach (var eventItem in allEventStore)
{
    if (eventItem is SkuQuantityAllocated allocatedEvent)
    {
        Console.WriteLine($"Event: SKU {allocatedEvent.SkuId} allocated: QTY {allocatedEvent.Quantity} to Order: {allocatedEvent.OrderId} EventTyp: {allocatedEvent.EventTyp} LineNumber:{allocatedEvent.LineNumber} at {allocatedEvent.Timestamp}");
    }
   
}


Console.WriteLine("List of all orders after allocation process.............................." + "\n\n\n");
foreach (var order in await orderServiceImpl.GetAllAsListAsync())
{
    order.printOrderDetail();

}

*/


// manually order update

await adjustSkuQuantityService.AdjustSkuQuantityManually("skuId_04", 20); // start Manually Process




Console.WriteLine("List of all orders after Manually process.............................." + "\n\n\n");
foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");
    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName} , isloked: {stcok.GetIsLocked}, alocatedqtey: {stcok.GetAllocatedQuantity()}");
}







/**
Console.WriteLine("deallocation all allocated order automatic.............................." + "\n\n\n");

foreach (var order in await  orderCancellationService.orderRepository.GetAllAsListAsync())
{
    Console.WriteLine($"date:{order.GetOrderDate()} orderi: {order.GetId()}");
      orderCancellationService.CancelOrder(order.GetId()).Wait();
      
}









foreach (var order in await allocationService.GetAllOrders())
{
    order.printOrderDetail();

}


foreach (var (key, values) in allocationService.timeTravel)
{

foreach (var (allocateQty, orderid) in values)
{
    Console.WriteLine($"  SKU: {key.SkuId},  Product Name: {key.productName}, AllocatedQty: {key.GetAllocatedQuantity()},    allocatedQty: {allocateQty}, OrderId: {orderid}");
}
}




Console.WriteLine("List of all orders after deallocation process.............................." + "\n\n\n");
foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");
    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.productName} , isloked: {stcok.isLocked}, alocatedqtey: {stcok.GetAllocatedQuantity()}, removed: {stcok.removedQuantity}");
}


Console.WriteLine("List of all orders after allocation process.............................." + "\n\n\n");
foreach (var order in await orderServiceImpl.GetAllAsListAsync())
{
    order.printOrderDetail();

}


**/