using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using vDieu.Dal.Contracts;

namespace vDieu.Dal
{
    public class Repository<T> : IRepository<T> where T : class
    {
        internal IDbContext Context;
        internal IDbSet<T> DbSet;

        public Repository(IDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public virtual IQueryable<T> GetDbQueryable()
        {
            return DbSet.AsQueryable();
        }

        public virtual IQueryable<T> GetDbQueryable(string includeProperties = "")
        {
            if (includeProperties == null) throw new ArgumentNullException("includeProperties");

            return includeProperties.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Aggregate<string, IQueryable<T>>(DbSet, (current, includeProperty) => current.Include(includeProperty));
        }

        public virtual IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = DbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public virtual T GetByID(object id)
        {
            return DbSet.Find(id);
        }

        public virtual T Insert(T entity)
        {
            return DbSet.Add(entity);
        }

        public virtual void Delete(object id)
        {
            T entityToDelete = DbSet.Find(id);
            Delete(entityToDelete);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (Context.Entry(entityToDelete).State == EntityState.Detached)
            {
                DbSet.Attach(entityToDelete);
            }
            DbSet.Remove(entityToDelete);
        }

        public virtual void Update(T oldEntity, T newEntity)
        {
            Context.Entry(oldEntity).CurrentValues.SetValues(newEntity);
        }
    }
}
