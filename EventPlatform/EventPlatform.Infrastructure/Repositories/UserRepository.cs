using EventPlatform.Application.Interfaces;
using EventPlatform.Domain.Model;
using EventPlatform.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Infrastructure.Repositories;

internal class UserRepository(AppDbContext _context) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Users.ToListAsync(cancellationToken);
    }

    public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }
        return user;
    }

    public async Task<User> GetUserByLoginHashedPassword(string login, string hashedPassword, CancellationToken cancellationToken)
    {
        // Здесь должна быть логика получения пользователя из базы данных
        var user= await _context.Users.SingleOrDefaultAsync(u => u.Login == login && u.PasswordHash == hashedPassword, cancellationToken);
        if(user == null)
        {
            throw new KeyNotFoundException($"Пользователь с логином {login} и предоставленным хешированным паролем не найден.");
        }
        return user;
    }

    public async Task AddAsync(User entity, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Guid id, User entity, CancellationToken cancellationToken)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
            _context.Users.Update(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"Пользователь с ID {id} не найден.");
        }
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
