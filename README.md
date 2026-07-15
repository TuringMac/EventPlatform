# Event Platform

## Запуск приложения

1. Получение `git clone -b sprint-3 https://github.com/TuringMac/EventPlatform`  
2. Сборка `dotnet build ./EventPlatform/EventPlatform.Api`  
3. Запуск `dotnet run --project ./EventPlatform/EventPlatform.Api --launch-profile "https"`  
4. API https://localhost:7068  
5. Swagger https://localhost:7068/swagger/index.html  
6. Тестирование `dotnet test ./EventPlatform/EventPlatform.Tests`  

## Описание API

### Endpoints

#### Events

GET `/api/events` Получение списка всех мероприятий в базе  
&emsp;Query параметры:  
&emsp;`title` - фильтр по заголовку (опционально)  
&emsp;`from` - фильтр событий с началом позже даты (опционально)  
&emsp;`to` - фильтр событий с концом до даты (опционально)  
&emsp;`page` - номер страницы (опционально 1)  
&emsp;`pageSize` - размер страницы (опционально 10)  
GET `/api/events/{id:guid}` Получение подробной информации по выбранному мероприятию  
POST `/api/events/{id}/book` Создание брони на мероприятие  
POST `/api/events` Добавление мероприятия в базу  
PUT `/api/events/{id:guid}` Обновление информации по мероприятию  
DELETE `/api/events/{id:guid}` Удаление мероприятия из базы  

#### Booking

GET `/api/bookings/{id}` Проверка состояния брони  

### Вывод структурирован моделью

```json
{
  "data": object,
  "success": bool,
  "statusCode": int,
  "dateTime": datetime,
  "message": string
}
```  

Пример: `https://localhost:7068/api/Events?from=2026-07-12T09%3A00%3A00&page=2&pageSize=3`

```json
{
  "data": {
    "totalItems": 5,
    "data": [
      {
        "id": "e467e0e1-76b6-4f39-afb4-778c55cb8afe",
        "title": "Вечер стендапа",
        "description": null,
        "startAt": "2026-07-15T12:00:00",
        "endAt": "2026-07-15T22:00:00"
      },
      {
        "id": "1dcf02ae-eab6-42f9-aaf3-e2ad5dcfa6f3",
        "title": "Закрытие летнего сезона",
        "description": null,
        "startAt": "2026-07-16T06:00:00",
        "endAt": "2026-07-16T20:00:00"
      }
    ],
    "currentPage": 2,
    "pageItems": 2
  },
  "success": true,
  "statusCode": 200,
  "dateTime": "2026-06-30T16:23:34.8362146Z",
  "message": "Получаем все мероприятия из коллекции"
}
```

### Формат ошибок

Формат ошибок стандартизирован Problem Details (RFC 7807)  

Пример: `https://localhost:7068/api/Events?page=-2&pageSize=3`  

```json
{
  "type": "ArgumentException",
  "title": "An error occurred",
  "status": 400,
  "detail": "Номер страницы должен быть положительным (Parameter 'page')",
  "instance": "/api/Events"
}
```

### Модели

#### Event

```json
{
  "id": "10814960-d812-4720-9492-b896930ff39e",
  "title": "Футбол",
  "description": null,
  "startAt": "2026-07-01T10:00:00",
  "endAt": "2026-07-01T16:00:00"
}
```

#### Booking

```json
{
  "id": "423862fb-f009-4ad9-b3a7-31efbfa2137e",
  "eventId": "10814960-d812-4720-9492-b896930ff39e",
  "status": 1,
  "createdAt": "2026-07-14T16:59:38.6029805Z",
  "processedAt": "2026-07-14T16:59:43.5878079Z"
}
```

Status (0 - Pending, 1 - Confirmed, 2 - Rejected)

## Логика

### BookingBackgroundService

1. Сервис проверяет брони в статусе Pending каждые 3сек.
2. Получает Pending бронь и принимает её на обработку.
3. После обработки переводит в статус Confirmed.
4. Переходит к пункту 1

### Сценарии использования

#### Бронирование на мероприятие

1. Пользователь выбирает мероприятие
2. Запрашивает бронь на это мероприятие
3. Проверяет обновление статуса брони запросом

## Changelog

### Sprint-3

 - Тесты для сервиса бронирования
 - Endpoints контроллеры и регистрация в DI
 - Реализация сервиса бронирования и хранилища в памяти
 - Определение интерфейсов для бронирования
 - Рефакторинг системыф обработки ошибок и форматирование текста

### Sprint-2

 - Написаны тесты  
 - Пагинация  
 - Фильтрация данных  
 - Глобальная обработка ошибок через middleware  

### Sprint-1

 - Добавлена валидация Id запроса и Id модели при обновлении ресурса
 - Исправлена валидация StartAt, EndAt
 - Маршруты API актуализированы в документации
 - Поле Description не обязательное
 - Исправлены HTTP коды ответа
 - Добавлена вариативность в сервис
 - Используется DTO на границе контроллера и бизнес-логики
 - Добавлен Swagger
 - Объявлены зависимости с указанием срока жизни
 - Реализован эндпоинт со структурированным ответом
 - Реализованы интерфейсы
 - Разработаны интерфейсы бизнес-логики и нфраструктуры
 - Пустой проект  