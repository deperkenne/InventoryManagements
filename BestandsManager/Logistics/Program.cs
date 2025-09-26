using BestandsManager.Logistics.Logic;
using BestandsManager.Logistics.Logics.Impl;
using BestandsManager.Logistics.Repositories.Impl;
using BestandsManager.Logistics.Repository;






InMemoryOrderImpl orderServiceImpl = new InMemoryOrderImpl();

InMemoryStockImpl stockServiceImpl = new InMemoryStockImpl();

IEventStoreRepository eventStoreRepository = new InMemoryEventStoreImpl();

stockServiceImpl.GetAvailableSkusAsync("ORANGE_L1").Wait();

Console.WriteLine("List of all orders after allocation process.............................." + "\n\n\n");

IOrderCancellationService orderCancellationService = new OrderCancellationServiceImpl(eventStoreRepository, orderServiceImpl, stockServiceImpl);

IOrderAllocationService allocationService = new OrderAllocationServiceImpl(orderServiceImpl, stockServiceImpl, eventStoreRepository, orderCancellationService);

IAdjustSkuQuantityManuellyService adjustSkuQuantityService = new AdjustSkuQuantityManuellyServiceImpl(stockServiceImpl, orderServiceImpl, eventStoreRepository, allocationService);

Console.WriteLine("List of all SKUs before automatic and manual processing:\n\n\n");

foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");

    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName}  , isloked:  {stcok.GetIsLocked} , alocatedqtey:  {stcok.GetAllocatedQuantity()}");
}

await allocationService.ProcessMultipleOrdersForAllocationAsync(); // start the automatic process

var allEventStore = await eventStoreRepository.GetAllEventsAsync();


Console.WriteLine("List of all Skus  after automatic process:\n\n\n");

foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");

    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName} , isloked: {stcok.GetIsLocked}, alocatedqtey: {stcok.GetAllocatedQuantity()}");
}

await adjustSkuQuantityService.AdjustSkuQuantityManuallyAsync("skuId_04", 20); // start  Manually Process

Console.WriteLine("List of all Skus after Manually process:\n\n\n");

foreach (var stcok in stockServiceImpl.skuInMemory)
{
    Console.WriteLine("After allocation process");

    Console.WriteLine($"SKU: {stcok.SkuId}, Available Quantity: {stcok.ProductName} , isloked: {stcok.GetIsLocked}, alocatedqtey: {stcok.GetAllocatedQuantity()}");
}







