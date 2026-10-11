# Event Platform

## Требования

PostgreSQL

## Запуск приложения

1. Получение `git clone -b sprint-8 https://github.com/TuringMac/EventPlatform`  
2. Сборка `dotnet build ./EventPlatform/`  
3. Строка подключения в файле **appsettings.Development.json**: `Host=<server>;Port=5432;Database=<db_name>;Username=postgres;Password=postgres` где:  
3.1. Host - адрес сервера;  
3.2. Database название БД;  
3.3. Username/Password учетные данные подключаемого пользователя.  
4. Запуск `dotnet run --project ./EventPlatform/EventPlatform.Api --launch-profile "https"`  
5. БД создается автоматически Миграциями  
5.1. Инициализируется первый пользователь-админ с параметрами из конфигурации (Admin:Username)  
6. API https://localhost:7068  
7. Swagger https://localhost:7068/swagger/index.html  
8. Тестирование `dotnet test ./EventPlatform/`  
8.1. Юнит-тесты `dotnet test ./EventPlatform/EventPlatform.Tests`  
8.2. Интеграционные тесты (требуется docker) `dotnet test ./EventPlatform/EventPlatform.IntegrationTests`  

## Роли и авторизация

- Без токена доступны просмотр мероприятий (`GET /api/events`, `GET /api/events/{id}`), проверка состояния (`GET /api/health`), регистрация (`POST /api/auth/register`) и вход (`POST /api/auth/login`). Остальные маршруты API требуют JWT: без него возвращается 401, при недостаточных правах — 403.
- `User` может бронировать мероприятие (`POST /api/events/{eventId}/book`), просматривать свою бронь (`GET /api/bookings/{id}`) и отменять свою бронь через `DELETE /api/events/{eventId}/book`. Чужие брони ему недоступны.
- `Admin` также может создавать, изменять и удалять мероприятия (`POST`, `PUT`, `DELETE /api/events`), просматривать все брони мероприятия и пользователей, управлять пользователями и отменять любую бронь по `DELETE /api/bookings/{bookingId}`.
- Публичная регистрация создаёт только пользователя с ролью `User`. Учётная запись `Admin` создаётся при инициализации базы из настроек `Admin:Username` и `Admin:Password` (в `appsettings.Development.json` указаны значения для локальной разработки).

### Получение JWT через Swagger

1. Запустите API с профилем `https` и откройте `https://localhost:7068/swagger/index.html` (Swagger доступен в окружении Development).
2. При необходимости выполните `POST /api/auth/register` с JSON-полями `login` и `password`, чтобы создать пользователя с ролью `User`. Для входа как `Admin` используйте учётные данные из настроек `Admin`.
3. Выполните `POST /api/auth/login` с теми же `login` и `password`. Скопируйте значение `token` из ответа.
4. Нажмите **Authorize**, вставьте токен **без** префикса `Bearer` и подтвердите. Теперь можно вызывать защищённые маршруты; для смены роли войдите под другой учётной записью и повторите авторизацию.

### Конфигурация JWT

Параметры задаются в секции `Jwt`: `Issuer` (издатель), `Audience` (аудитория), `Key` (секрет подписи) и `Lifetime` (время жизни токена в минутах). Для локальной разработки `Issuer`, `Audience` и `Key` указаны в `EventPlatform/EventPlatform.Api/appsettings.Development.json`, а `Lifetime` (15 минут) — в `appsettings.json`. Для продакшна задайте собственный длинный случайный секрет `Jwt:Key` через переменную окружения `Jwt__Key` или защищённое хранилище секретов, а не храните его в репозитории; также настройте `Jwt:Issuer` и `Jwt:Audience` для своего окружения.

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
GET `/api/events/{eventId:guid}/bookings` Обзор Броней конкретного мероприятия  
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
        "endAt": "2026-07-15T22:00:00",
        "totalSeats": 4,
        "availableSeats": 0
      },
      {
        "id": "1dcf02ae-eab6-42f9-aaf3-e2ad5dcfa6f3",
        "title": "Закрытие летнего сезона",
        "description": null,
        "startAt": "2026-07-16T06:00:00",
        "endAt": "2026-07-16T20:00:00",
        "totalSeats": 2,
        "availableSeats": 1
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
  "endAt": "2026-07-01T16:00:00",
  "totalSeats": 100,
  "availableSeats": 15
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

Status (0 - Pending, 1 - Confirmed, 2 - Rejected, 3 - Cancelled)

## Логика

### BookingBackgroundService

1. Сервис проверяет брони в статусе Pending каждые 3сек.
2. Получает Pending бронь и принимает её на обработку.
3. После обработки переводит в статус Confirmed/Reject.
4. Переходит к пункту 1

Используется одновременная обработка нескольких броней.  

### Сценарии использования

#### Бронирование на мероприятие

1. Пользователь выбирает мероприятие
2. Запрашивает бронь на это мероприятие
3. Место резервируется
4. Ожидает подтверждения или отказа в бронировании
5. Во втором случае место освобождается

## Доступ к данным

## База данных

### Репозиторный слой

Доступ к данным инкапсулирован в репозиториях `EventRepository` и `BookingRepository`. Репозитории реализуют интерфейсы `IEventRepository` и `IBookingRepository`, используют `AppDbContext`, а бизнес-логика взаимодействует с ними через эти интерфейсы.

Интеграционные тесты проверяют репозитории на реальном PostgreSQL. Для этого используется Testcontainers: перед запуском тестов автоматически создается и запускается контейнер PostgreSQL, после чего применяются миграции базы данных. Для работы Testcontainers во время выполнения `dotnet test` должен быть запущен Docker.

### Управление миграциями

Создание миграций `dotnet ef migrations add user_scheme --project .\EventPlatform\EventPlatform.Infrastructure --startup-project .\EventPlatform\EventPlatform.Api`  
Ручное выполнение миграций `dotnet ef database update --project .\EventPlatform\EventPlatform.Infrastructure --startup-project .\EventPlatform\EventPlatform.Api`  

## Структура проекта

Проект разделен на слои по принципам Clean Architecture. Зависимости направлены от внешних слоев к внутренним: API использует Application и Infrastructure, Application использует Domain, а Domain не зависит от остальных слоев.

### EventPlatform.Domain

Ядро предметной области. Содержит сущности `Event` и `Booking`, перечисления, доменные исключения и базовые доменные интерфейсы. Этот слой не знает о базе данных, ASP.NET Core, инфраструктуре или способе доставки запросов.

### EventPlatform.Application

Слой сценариев использования и бизнес-логики приложения. Содержит:

- интерфейсы сервисов и репозиториев;
- сервисы мероприятий и бронирований;
- DTO для обмена данными с API;
- `BookingBackgroundService`, который периодически обрабатывает ожидающие бронирования.

Слой определяет контракты (`IEventService`, `IBookingService`, `IEventRepository`, `IBookingRepository`), но не содержит конкретной реализации доступа к PostgreSQL.

### EventPlatform.Infrastructure

Слой реализации внешних технических зависимостей. Содержит:

- `AppDbContext` и конфигурации сущностей Entity Framework Core;
- реализации `EventRepository` и `BookingRepository`;
- PostgreSQL-провайдер Npgsql;
- миграции базы данных.

Регистрация `DbContext` и репозиториев выполняется методом `AddInfrastructure`.

### EventPlatform.Api

Внешний HTTP-слой приложения ASP.NET Core. Содержит контроллеры мероприятий, бронирований и проверки состояния, DTO-ответы, обработчик исключений и конфигурацию HTTP pipeline. Регистрация API выполняется методом `AddPresentation`.

При запуске API регистрирует Application и Infrastructure, применяет миграции базы данных и публикует HTTP endpoints.

### EventPlatform.Tests

Модульные тесты сервисов Application. Для изоляции от PostgreSQL используют EF Core In-Memory и проверяют сценарии мероприятий, бронирований, ограничения мест и конкурентную обработку.

### EventPlatform.IntegrationTests

Интеграционные тесты репозиториев Infrastructure на реальном PostgreSQL. База запускается в контейнере через Testcontainers, поэтому перед запуском этих тестов должен быть доступен Docker. Поддерживается запуск тестовых контейнеров в среде WSL с установленным Docker Engine.

## Changelog

### Sprint-8

- Доработаны и добавлены тесты
- Все изменяемые параметры вынесены в конфигурацию
- Нарушения бизнес-правил сопровождается специфическим исключением
- Бизнес-правила расширены проверкой даты события и лимита броней для пользователя
- Созданы миграции для поддержания БД в актуальном состоянии
- Бронь принадлежит пользователю
- Модель пользователя с соответствующей таблицей, репозиторием и сервисом  
- Аутентификация, Авторизация, генерация токенов  

### Sprint-7

- Разделение решения на логические проекты, соответствующие уровням приложения (Domain, Application, Infrastructure, Presentation)

### Sprint-6

- Добавлены интеграционные тесты
- Юнит-тесты адаптированы к репозиториям
- БД создается и актуализируется миграциями
- Используются репозитории для доступа к БД

### Sprint-5

- Тесты используют EFCore In-Memory
- Переход на асинхронные вызовы
- Использование БД Postgres

### Sprint-4

- Обработка брони присваивает статус Confirmed/Rejected
- Обработка брони происходит одновременно
- Мероприятие получило число мест
- Исключение NoAvailableSeats для случая овербукинга

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