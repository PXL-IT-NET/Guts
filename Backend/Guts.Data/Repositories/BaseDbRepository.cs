using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public abstract class BaseDbRepository<T> : IBasicRepository<T> where T : class, IDomainObject
    {
        protected readonly GutsContext _context;

        protected BaseDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
            {
                throw new DataNotFoundException();
            }
            return entity;
        }

        public async Task<T> AddAsync(T newEntity)
        {
            if (newEntity.Id > 0)
            {
                throw new ArgumentException("Cannot add an existing enity.");
            }

            var entry = await _context.Set<T>().AddAsync(newEntity);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<IList<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }
    }
}