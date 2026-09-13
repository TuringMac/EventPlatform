using EventPlatform.Api.DbContexts;
using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Api.Repositories;

public class BookingRepository(AppDbContext _context) : IBookingRepository
{
    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batch, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatusEnum.Pending)
            .OrderBy(b => b.CreatedAt)
            .Take(batch)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(booking).State == EntityState.Detached)
            _context.Bookings.Update(booking);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
