using Microsoft.EntityFrameworkCore.Storage;
using UretimAPI.Data;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Repositories.Implementations;

namespace UretimAPI.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UretimDbContext _context;
        private IDbContextTransaction? _transaction;
        private readonly Dictionary<Type, object> _repositories;

        public UnitOfWork(UretimDbContext context)
        {
            _context = context;
            _repositories = new Dictionary<Type, object>();
        }

        // Lazy loading repositories
        public IProductRepository Products => GetRepository<IProductRepository>(() => new ProductRepository(_context));
        public IOperationRepository Operations => GetRepository<IOperationRepository>(() => new OperationRepository(_context));
        public ICycleTimeRepository CycleTimes => GetRepository<ICycleTimeRepository>(() => new CycleTimeRepository(_context));
        public IProductionTrackingFormRepository ProductionTrackingForms => GetRepository<IProductionTrackingFormRepository>(() => new ProductionTrackingFormRepository(_context));
        public IPackingRepository Packings => GetRepository<IPackingRepository>(() => new PackingRepository(_context));
        public IOrderRepository Orders => GetRepository<IOrderRepository>(() => new OrderRepository(_context));

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            return GetRepository<IGenericRepository<T>>(() => new GenericRepository<T>(_context));
        }

        private T GetRepository<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = factory();
            }
            return (T)_repositories[type];
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}