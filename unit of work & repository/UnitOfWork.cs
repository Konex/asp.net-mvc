using System;
using System.Collections;
using vDieu.Dal.Contracts;

namespace vDieu.Dal
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContext _context;
        private Hashtable _repositories;
        private bool _disposed;

        public UnitOfWork(IDbContext context)
        {
            _context = context;
        }

        public UnitOfWork()
        {
            _context = new vDbContext();
        }

        public IRepository<T> Repository<T>() where T : class
        {
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(Repository<>);

                var repositoryInstance =
                    Activator.CreateInstance(repositoryType
                            .MakeGenericType(typeof(T)), _context);

                _repositories.Add(type, repositoryInstance);
            }

            return (IRepository<T>)_repositories[type];
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
