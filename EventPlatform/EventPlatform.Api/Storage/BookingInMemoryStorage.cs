using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Storage;

public class BookingInMemoryStorage : IBookingStorage
{
    List<Booking> _bookings { get; } = new();

    public BookingInMemoryStorage()
    {
        // Добавим несколько тестовых бронирований
        var evt = new Booking { EventId = new Guid("{10814960-D812-4720-9492-B896930FF39E}") };
        evt.Confirm();
        _bookings.Add(evt);
        _bookings.Add(new Booking { EventId = new Guid("{10814960-D812-4720-9492-B896930FF39E}") });
    }

    public void Add(Booking obj)
    {
        _bookings.Add(obj);
    }

    public void Delete(Guid id)
    {
        var obj = _bookings.FirstOrDefault(b => b.Id == id);
        if (obj == null)
            throw new KeyNotFoundException($"Бронь с ID {id} не найдена");
        _bookings.Remove(obj);
    }

    public IEnumerable<Booking> GetAll()
    {
        return _bookings;
    }

    public Booking GetById(Guid id)
    {
        var obj = _bookings.FirstOrDefault(b => b.Id == id);
        if (obj == null)
            throw new KeyNotFoundException($"Бронь с ID {id} не найдена");
        return obj;
    }

    public void Update(Guid id, Booking obj)
    {
        var oldObj = _bookings.FirstOrDefault(b => b.Id == id);
        if (oldObj == null)
            throw new KeyNotFoundException($"Бронь с ID {id} не найдена");

        oldObj.EventId = obj.EventId;
    }
}
