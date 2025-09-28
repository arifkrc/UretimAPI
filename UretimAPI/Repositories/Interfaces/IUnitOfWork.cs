using UretimAPI.Repositories.Interfaces;

namespace UretimAPI.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        // Repository Properties
        IProductRepository Products { get; }
        IOperationRepository Operations { get; }
        ICycleTimeRepository CycleTimes { get; }
        IProductionTrackingFormRepository ProductionTrackingForms { get; }
        IPackingRepository Packings { get; }
        IOrderRepository Orders { get; }
        IShipmentRepository Shipments { get; }

        // Transaction Methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        // Generic Repository Access
        IGenericRepository<T> Repository<T>() where T : class;
    }
}