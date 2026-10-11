using EventPlatform.Application.Interfaces;
using EventPlatform.Infrastructure.DbContexts;
using EventPlatform.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Infrastructure.Repositories;

public class BookingRepository(AppDbContext _context) : IBookingRepository
{
    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await _context.Bookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountUserBookings(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Bookings.CountAsync(b => b.UserId == userId && b.Status != BookingStatusEnum.Cancelled, cancellationToken);
    }

    public async Task<Guid> GetBookingIdByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Where(b => b.EventId == eventId && b.UserId == userId)
            .Where(b => b.Status == BookingStatusEnum.Pending || b.Status == BookingStatusEnum.Confirmed) // Скипаем отмененные бронирования
            .OrderByDescending(b => b.CreatedAt) // На текущем этапе бизнес логики, отменяем самую свежую бронь. Может и не придется обрабатывать воркеру.
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return booking;
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await _context.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batch, CancellationToken cancellationToken)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatusEnum.Pending)
            .OrderBy(b => b.CreatedAt)
            .Take(batch)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken)
    {
        if (_context.Entry(booking).State == EntityState.Detached)
            _context.Bookings.Update(booking);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
