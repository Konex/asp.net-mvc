using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace vDieu.Dal.Contracts
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetDbQueryable();
        IQueryable<T> GetDbQueryable(string includeProperties = "");
        IEnumerable<T> GetAll(Expression<Func<T, bool>> filter = null, 
                              Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
                              string includeProperties = "");
        T GetByID(object id);
        T Insert(T e);
        void Delete(object id);
        void Delete(T e);
        void Update(T oE, T nE);
    }
}
