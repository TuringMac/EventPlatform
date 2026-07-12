using EventPlatform.Api.Interfaces;
using EventPlatform.Api.Model;

namespace EventPlatform.Api.Storage;

public class EventInMemoryStorage : IEventStorage
{
    List<Event> _events { get; } = new List<Event>();

    public EventInMemoryStorage()
    {
        _events.Add(new Event { Id = new Guid("{00688318-0ECB-4444-BCB8-FCDCCD16103C}"), Title = "Квадроциклы", StartAt = DateTime.Parse("2026-06-30 12:00"), EndAt = DateTime.Parse("2026-07-01 13:00") });
        _events.Add(new Event { Id = new Guid("{1371A164-F0C0-4AF3-97FF-5E59BA78B70B}"), Title = "Велосипеды", StartAt = DateTime.Parse("2026-07-01 11:00"), EndAt = DateTime.Parse("2026-07-01 13:00") });
        _events.Add(new Event { Id = new Guid("{10814960-D812-4720-9492-B896930FF39E}"), Title = "Футбол", StartAt = DateTime.Parse("2026-07-01 10:00"), EndAt = DateTime.Parse("2026-07-01 16:00") });

        // AI generated
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Турнир по шахматам", StartAt = DateTime.Parse("2026-06-29 10:00"), EndAt = DateTime.Parse("2026-06-29 14:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Хакатон 'Лето 2026'", StartAt = DateTime.Parse("2026-06-29 15:00"), EndAt = DateTime.Parse("2026-06-29 18:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Фестиваль воздушных змеев", StartAt = DateTime.Parse("2026-06-30 09:00"), EndAt = DateTime.Parse("2026-06-30 12:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Мастер-класс по гончарному делу", StartAt = DateTime.Parse("2026-06-30 14:00"), EndAt = DateTime.Parse("2026-06-30 17:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Кинопоказ под открытым небом", StartAt = DateTime.Parse("2026-07-01 10:00"), EndAt = DateTime.Parse("2026-07-01 18:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Йога в парке", StartAt = DateTime.Parse("2026-07-02 11:00"), EndAt = DateTime.Parse("2026-07-02 15:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Соревнования по плаванию", StartAt = DateTime.Parse("2026-07-03 12:00"), EndAt = DateTime.Parse("2026-07-03 14:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Концерт джазовой музыки", StartAt = DateTime.Parse("2026-07-04 21:00"), EndAt = DateTime.Parse("2026-07-04 23:30") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Выставка современных искусств", StartAt = DateTime.Parse("2026-07-05 08:00"), EndAt = DateTime.Parse("2026-07-05 09:30") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Марафон 'Белые ночи'", StartAt = DateTime.Parse("2026-07-06 10:00"), EndAt = DateTime.Parse("2026-07-06 13:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Лекция по квантовой физике", StartAt = DateTime.Parse("2026-07-07 19:00"), EndAt = DateTime.Parse("2026-07-07 22:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Пикник на берегу озера", StartAt = DateTime.Parse("2026-07-08 11:00"), EndAt = DateTime.Parse("2026-07-08 19:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Воркшоп по мобильной разработке", StartAt = DateTime.Parse("2026-07-09 07:00"), EndAt = DateTime.Parse("2026-07-09 14:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Турнир по настольному теннису", StartAt = DateTime.Parse("2026-07-10 15:00"), EndAt = DateTime.Parse("2026-07-10 17:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Экскурсия по историческому центру", StartAt = DateTime.Parse("2026-07-11 12:00"), EndAt = DateTime.Parse("2026-07-11 18:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Фестиваль уличной еды", StartAt = DateTime.Parse("2026-07-12 09:00"), EndAt = DateTime.Parse("2026-07-12 18:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Поход в горы", StartAt = DateTime.Parse("2026-07-13 10:00"), EndAt = DateTime.Parse("2026-07-13 15:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Соревнования по картингу", StartAt = DateTime.Parse("2026-07-14 11:00"), EndAt = DateTime.Parse("2026-07-14 13:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Вечер стендапа", StartAt = DateTime.Parse("2026-07-15 12:00"), EndAt = DateTime.Parse("2026-07-15 22:00") });
        _events.Add(new Event { Id = Guid.NewGuid(), Title = "Закрытие летнего сезона", StartAt = DateTime.Parse("2026-07-16 06:00"), EndAt = DateTime.Parse("2026-07-16 20:00") });
    }

    public void Add(Event obj)
    {
        _events.Add(obj);
    }

    public void Delete(Guid id)
    {
        var obj = _events.FirstOrDefault(e => e.Id == id);
        if (obj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        _events.Remove(obj);
    }

    public IEnumerable<Event> GetAll()
    {
        return _events;
    }

    public Event GetById(Guid id)
    {
        var @event = _events.FirstOrDefault(e => e.Id == id);
        if (@event == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");
        return @event;
    }

    public void Update(Guid id, Event newObj)
    {
        var oldObj = _events.FirstOrDefault(e => e.Id == id);
        if (oldObj == null)
            throw new KeyNotFoundException($"Мероприятие с ID {id} не найдено");

        oldObj.Title = newObj.Title;
        oldObj.Description = newObj.Description;
        oldObj.StartAt = newObj.StartAt;
        oldObj.EndAt = newObj.EndAt;
    }
}
