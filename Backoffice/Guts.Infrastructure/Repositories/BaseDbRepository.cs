using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Application.Repositories;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal abstract class BaseDbRepository<T, TConcrete> : IBasicRepository<T> where T : IEntity where TConcrete : class, T
{
    protected readonly GutsContext _context;

    protected BaseDbRepository(GutsContext context)
    {
        _context = context;
    }

    public async Task<T> GetByIdAsync(params object[] idValues)
    {
        var entity = await _context.Set<TConcrete>().FindAsync(idValues);
        if (entity == null)
        {
            throw new DataNotFoundException();
        }
        return entity;
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _context.Set<TConcrete>().OfType<T>().ToListAsync();
    }

    public async Task<T> AddAsync(T newEntity)
    {
        var idValues = GetEntityId(newEntity);
        if (HasGeneratedKeyAlreadySet(idValues))
        {
            throw new ArgumentException("Cannot add an existing entity (identifier is not empty).");
        }

        var entry = await _context.Set<TConcrete>().AddAsync((TConcrete)newEntity);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public virtual async Task<T> UpdateAsync(T existingEntity)
    {
        object[] idValues = GetEntityId(existingEntity);
        if (HasMissingOrZeroKey(idValues))
        {
            throw new ArgumentException("Cannot update a non-existing entity (identifier is empty).");
        }

        var entry = _context.Set<TConcrete>().Update((TConcrete)existingEntity);
        await _context.SaveChangesAsync();

        return entry.Entity;
    }

    protected virtual object[] GetEntityId(T entity)
    {
        return entity?.IdentityValues ?? Array.Empty<object>();
    }

    private bool HasGeneratedKeyAlreadySet(object[] keyValues)
    {
        var key = _context.Model.FindEntityType(typeof(TConcrete))?.FindPrimaryKey();
        if (key == null || key.Properties.Count != keyValues.Length)
        {
            return keyValues.Length == 1 && IsPositiveKeyValue(keyValues[0]);
        }

        for (int i = 0; i < key.Properties.Count; i++)
        {
            if (key.Properties[i].ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd
                && IsPositiveKeyValue(keyValues[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasMissingOrZeroKey(object[] keyValues)
    {
        if (keyValues == null || keyValues.Length == 0)
        {
            return true;
        }

        var key = _context.Model.FindEntityType(typeof(TConcrete))?.FindPrimaryKey();
        if (key == null || key.Properties.Count != keyValues.Length)
        {
            return IsEmptyOrDefaultKeyValue(keyValues[0]);
        }

        for (int i = 0; i < key.Properties.Count; i++)
        {
            if (IsEmptyOrDefaultKeyValue(keyValues[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPositiveKeyValue(object val) =>
        val switch
        {
            int intVal => intVal > 0,
            long longVal => longVal > 0,
            Guid guidVal => guidVal != Guid.Empty,
            _ => false
        };

    private static bool IsEmptyOrDefaultKeyValue(object val) =>
        val switch
        {
            null => true,
            int intVal => intVal <= 0,
            long longVal => longVal <= 0,
            Guid guidVal => guidVal == Guid.Empty,
            string strVal => string.IsNullOrWhiteSpace(strVal),
            _ => false
        };

    public virtual async Task DeleteAsync(T entityToDelete)
    {
        _context.Set<TConcrete>().Remove((TConcrete)entityToDelete);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteByIdAsync(params object[] idValues)
    {
        var entityToDelete = await _context.Set<TConcrete>().FindAsync(idValues);
        await DeleteAsync(entityToDelete);
    }

    public async Task DeleteBulkAsync(IEnumerable<T> entitiesToDelete)
    {
        _context.Set<TConcrete>().RemoveRange((IEnumerable<TConcrete>)entitiesToDelete);
        await _context.SaveChangesAsync();
    }

    protected async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        if (!_context.Database.IsRelational())
        {
            await operation();
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await operation();
            await transaction.CommitAsync();
        });
    }
}