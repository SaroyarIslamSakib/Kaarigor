using CleanSolution.Domain.Entities;
using CleanSolution.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace CleanSolution.Infrastructure.Repositories
{
    public class Repository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot<TKey>
        where TKey : IComparable
    {
        private DbContext _dbContext;
        private DbSet<TAggregateRoot> _dbSet;

        public Repository(DbContext context)
        {
            _dbContext = context;
            _dbSet = _dbContext.Set<TAggregateRoot>();
        }

        public virtual async Task AddAsync(TAggregateRoot entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task RemoveAsync(TKey id)
        {
            var entityToDelete = _dbSet.Find(id);
            await RemoveAsync(entityToDelete);
        }

        public virtual async Task RemoveAsync(TAggregateRoot entityToDelete)
        {
            await Task.Run(() =>
            {
                if (_dbContext.Entry(entityToDelete).State == EntityState.Detached)
                {
                    _dbSet.Attach(entityToDelete);
                }
                _dbSet.Remove(entityToDelete);
            });
        }

        public virtual async Task RemoveAsync(Expression<Func<TAggregateRoot, bool>> filter)
        {
            await Task.Run(() =>
            {
                _dbSet.RemoveRange(_dbSet.Where(filter));
            });
        }
        public async Task RemoveAllAsync()
        {
            var entities = await _dbSet.ToListAsync();

            if (entities.Any())
            {
                _dbSet.RemoveRange(entities);
            }
        }

        public virtual async Task EditAsync(TAggregateRoot entityToUpdate)
        {
            await Task.Run(() =>
            {
                _dbSet.Attach(entityToUpdate);
                _dbContext.Entry(entityToUpdate).State = EntityState.Modified;
            });
        }

        public virtual async Task<TAggregateRoot> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<int> GetCountAsync(Expression<Func<TAggregateRoot, bool>> filter = null)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            int count;

            if (filter is not null)
                count = await query.CountAsync(filter);
            else
                count = await query.CountAsync();

            return count;
        }

        public virtual int GetCount(Expression<Func<TAggregateRoot, bool>>? filter = null)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            int count;

            if (filter is not null)
                count = query.Count(filter);
            else
                count = query.Count();

            return count;
        }

        public virtual async Task<IList<TAggregateRoot>> GetAsync(Expression<Func<TAggregateRoot, bool>> filter,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>>? include = null)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            return await query.ToListAsync();
        }

        public virtual async Task<IList<TAggregateRoot>> GetAllAsync()
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            return await query.ToListAsync();
        }

        public virtual async Task<(IList<TAggregateRoot> data, int total, int totalDisplay)> GetAsync(
            Expression<Func<TAggregateRoot, bool>>? filter = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>> orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            int pageIndex = 1,
            int pageSize = 10,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            var total = query.Count();
            var totalDisplay = query.Count();

            if (filter != null)
            {
                query = query.Where(filter);
                totalDisplay = query.Count();
            }

            if (include != null)
                query = include(query);

            IList<TAggregateRoot> data;

            if (orderBy != null)
            {
                var result = orderBy(query).Skip((pageIndex - 1) * pageSize).Take(pageSize);

                if (isTrackingOff)
                    data = await result.AsNoTracking().ToListAsync();
                else
                    data = await result.ToListAsync();
            }
            else
            {
                var result = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

                if (isTrackingOff)
                    data = await result.AsNoTracking().ToListAsync();
                else
                    data = await result.ToListAsync();
            }

            return (data, total, totalDisplay);
        }

        public virtual async Task<(IList<TAggregateRoot> data, int total, int totalDisplay)> GetDynamicAsync(
            Expression<Func<TAggregateRoot, bool>>? filter = null,
            string? orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            int pageIndex = 1,
            int pageSize = 10,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            var total = query.Count();
            var totalDisplay = query.Count();

            if (filter is not null)
            {
                query = query.Where(filter);
                totalDisplay = query.Count();
            }

            if (include is not null)
                query = include(query);

            IList<TAggregateRoot> data;

            if (orderBy is not null)
            {
                var result = query.OrderBy(orderBy).Skip((pageIndex - 1) * pageSize).Take(pageSize);

                if (isTrackingOff)
                    data = await result.AsNoTracking().ToListAsync();
                else
                    data = await result.ToListAsync();
            }
            else
            {
                var result = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

                if (isTrackingOff)
                    data = await result.AsNoTracking().ToListAsync();
                else
                    data = await result.ToListAsync();
            }

            return (data, total, totalDisplay);
        }

        public virtual async Task<IList<TAggregateRoot>> GetAsync(
            Expression<Func<TAggregateRoot, bool>> filter = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>> orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = orderBy(query);

                if (isTrackingOff)
                    return await result.AsNoTracking().ToListAsync();
                else
                    return await result.ToListAsync();
            }
            else
            {
                if (isTrackingOff)
                    return await query.AsNoTracking().ToListAsync();
                else
                    return await query.ToListAsync();
            }
        }

        public virtual async Task<IList<TAggregateRoot>> GetDynamicAsync(
            Expression<Func<TAggregateRoot, bool>> filter = null,
            string orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = query.OrderBy(orderBy);

                if (isTrackingOff)
                    return await result.AsNoTracking().ToListAsync();
                else
                    return await result.ToListAsync();
            }
            else
            {
                if (isTrackingOff)
                    return await query.AsNoTracking().ToListAsync();
                else
                    return await query.ToListAsync();
            }
        }

        public virtual void Add(TAggregateRoot entity)
        {
            _dbSet.Add(entity);
        }

        public virtual void Remove(TKey id)
        {
            var entityToDelete = _dbSet.Find(id);
            Remove(entityToDelete);
        }

        public virtual void Update(TAggregateRoot entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(TAggregateRoot entityToDelete)
        {
            if (_dbContext.Entry(entityToDelete).State == EntityState.Detached)
            {
                _dbSet.Attach(entityToDelete);
            }
            _dbSet.Remove(entityToDelete);
        }

        public virtual void Remove(Expression<Func<TAggregateRoot, bool>> filter)
        {
            _dbSet.RemoveRange(_dbSet.Where(filter));
        }

        public virtual void Edit(TAggregateRoot entityToUpdate)
        {
            if (!_dbSet.Local.Any(x => x == entityToUpdate))
            {
                _dbSet.Attach(entityToUpdate);
                _dbContext.Entry(entityToUpdate).State = EntityState.Modified;
            }
        }

        public virtual IList<TAggregateRoot> Get(Expression<Func<TAggregateRoot, bool>> filter,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            return query.ToList();
        }

        public virtual IList<TAggregateRoot> GetAll()
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            return query.ToList();
        }

        public virtual TAggregateRoot GetById(TKey id)
        {
            return _dbSet.Find(id);
        }

        public virtual (IList<TAggregateRoot> data, int total, int totalDisplay) Get(
            Expression<Func<TAggregateRoot, bool>> filter = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>> orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            int pageIndex = 1, int pageSize = 10, bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            var total = query.Count();
            var totalDisplay = query.Count();

            if (filter != null)
            {
                query = query.Where(filter);
                totalDisplay = query.Count();
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = orderBy(query).Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (isTrackingOff)
                    return (result.AsNoTracking().ToList(), total, totalDisplay);
                else
                    return (result.ToList(), total, totalDisplay);
            }
            else
            {
                var result = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (isTrackingOff)
                    return (result.AsNoTracking().ToList(), total, totalDisplay);
                else
                    return (result.ToList(), total, totalDisplay);
            }
        }

        public virtual (IList<TAggregateRoot> data, int total, int totalDisplay) GetDynamic(
            Expression<Func<TAggregateRoot, bool>> filter = null,
            string? orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            int pageIndex = 1, int pageSize = 10, bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;
            var total = query.Count();
            var totalDisplay = query.Count();

            if (filter != null)
            {
                query = query.Where(filter);
                totalDisplay = query.Count();
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = query.OrderBy(orderBy).Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (isTrackingOff)
                    return (result.AsNoTracking().ToList(), total, totalDisplay);
                else
                    return (result.ToList(), total, totalDisplay);
            }
            else
            {
                var result = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                if (isTrackingOff)
                    return (result.AsNoTracking().ToList(), total, totalDisplay);
                else
                    return (result.ToList(), total, totalDisplay);
            }
        }

        public virtual IList<TAggregateRoot> Get(Expression<Func<TAggregateRoot, bool>> filter = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>> orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = orderBy(query);

                if (isTrackingOff)
                    return result.AsNoTracking().ToList();
                else
                    return result.ToList();
            }
            else
            {
                if (isTrackingOff)
                    return query.AsNoTracking().ToList();
                else
                    return query.ToList();
            }
        }

        public virtual IList<TAggregateRoot> GetDynamic(Expression<Func<TAggregateRoot, bool>> filter = null,
            string orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>> include = null,
            bool isTrackingOff = false)
        {
            IQueryable<TAggregateRoot> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
            {
                var result = query.OrderBy(orderBy);

                if (isTrackingOff)
                    return result.AsNoTracking().ToList();
                else
                    return result.ToList();
            }
            else
            {
                if (isTrackingOff)
                    return query.AsNoTracking().ToList();
                else
                    return query.ToList();
            }
        }

        public async Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<TAggregateRoot, TResult>>? selector,
            Expression<Func<TAggregateRoot, bool>>? predicate = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>>? orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>>? include = null,
            bool disableTracking = true,
            CancellationToken cancellationToken = default) where TResult : class
        {
            var query = _dbSet.AsQueryable();
            if (disableTracking) query.AsNoTracking();
            if (include is not null) query = include(query);
            if (predicate is not null) query = query.Where(predicate);
            return orderBy is not null
                ? await orderBy(query).Select(selector!).ToListAsync(cancellationToken)
                : await query.Select(selector!).ToListAsync(cancellationToken);
        }

        public async Task<TResult> SingleOrDefaultAsync<TResult>(Expression<Func<TAggregateRoot, TResult>>? selector,
            Expression<Func<TAggregateRoot, bool>>? predicate = null,
            Func<IQueryable<TAggregateRoot>, IOrderedQueryable<TAggregateRoot>>? orderBy = null,
            Func<IQueryable<TAggregateRoot>, IIncludableQueryable<TAggregateRoot, object>>? include = null,
            bool disableTracking = true)
        {
            var query = _dbSet.AsQueryable();
            if (disableTracking) query.AsNoTracking();
            if (include is not null) query = include(query);
            if (predicate is not null) query = query.Where(predicate);
            return (orderBy is not null
                ? await orderBy(query).Select(selector!).FirstOrDefaultAsync()
                : await query.Select(selector!).FirstOrDefaultAsync())!;
        }
    }
}
