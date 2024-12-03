using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal abstract class BaseDbRepository<T, TConcrete> : IBasicRepository<T> where T : IEntity where TConcrete : class, T
    {
        protected readonly GutsContext _context;

        protected BaseDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _context.Set<TConcrete>().FindAsync(id);
            if (entity == null)
            {
                throw new DataNotFoundException();
            }
            return entity;
        }

        public virtual async Task<IList<T>> GetAllAsync()
        {
            return await _context.Set<TConcrete>().OfType<T>().ToListAsync();
        }

        public async Task<T> AddAsync(T newEntity)
        {
            if (newEntity.Id > 0)
            {
                throw new ArgumentException("Cannot add an existing entity (Id > 0).");
            }

            var entry = await _context.Set<TConcrete>().AddAsync((TConcrete)newEntity);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<T> UpdateAsync(T existingEntity)
        {
            if (existingEntity.Id <= 0)
            {
                throw new ArgumentException("Cannot update a non-existing entity (Id <= 0).");
            }

            var entry = _context.Set<TConcrete>().Update((TConcrete)existingEntity);
            await _context.SaveChangesAsync();

            return entry.Entity;
        }

        public virtual async Task DeleteAsync(T entityToDelete)
        {
            _context.Set<TConcrete>().Remove((TConcrete)entityToDelete);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteByIdAsync(int id)
        {
            var entityToDelete = await _context.Set<TConcrete>().FindAsync();
            await DeleteAsync(entityToDelete);
        }

        public async Task DeleteBulkAsync(IEnumerable<T> entitiesToDelete)
        {
            _context.Set<TConcrete>().RemoveRange((IEnumerable<TConcrete>)entitiesToDelete);
            await _context.SaveChangesAsync();
        }
    }
}