using System;
using System.Collections.Generic;
using vDieu.Dal.Contracts;

namespace vDieu.Dal
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContext _context;
        private Dictionary<string, object> _repositories;
		// A class-level variable is used to prevent Dispose being called 
		// multiple times by clients.
        private bool _disposed = false;

        public UnitOfWork(IDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : class
        {
            if (_repositories == null)
                _repositories = new Dictionary<string, object>();

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

		// Technically, we don't need an overloaded version of Dispose in here unless
		// your unit of work class accesses resources through unmanaged code, then
		// you would need to implement a Finalize method (commented out below).
		//
		// But here I just want to demonstrate in a more complicated scenario that your 
		// clean up code might need to access external objects then you should only do 
		// so during disposing.
        protected virtual void Dispose(bool disposing)
        {
			if (_disposed) return;
			
			if (disposing)
			{	
				// This unit of work object is being disposed but not finalized yet.
				// So it is still safe to access other objects (except the base object)
				// only from inside this code block.
				_context.Dispose();
			}

			// Perform cleanup tasks here that need to be done in either Dispose and Finalize
			// ...	
        }

        public void Dispose()
        {
            Dispose(true);
			_disposed = true;
			// Because calling Finalize method has a performance hit,
			// so we tell .NET not to call the Finalize method
			// after we have disposed the DbContext.
            GC.SuppressFinalize(this);
        }
		
		// ~UnitOfWork()
		// {
		// 	Dispose(false);
		// }
    }
}