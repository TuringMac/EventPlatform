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

    public async Task<Booking> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
    public async Task<Guid> GetBookingIdByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return await _context.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Bookings
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
