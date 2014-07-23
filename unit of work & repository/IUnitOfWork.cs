using System;

namespace vDieu.Dal.Contracts
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        void Save();
        void Dispose();
    }
}
