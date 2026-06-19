# Event Platform  
## Запуск приложения  

1. `git clone -b sprint-1 https://github.com/TuringMac/EventPlatform`  
2. Linux-style `dotnet run --project ./EventPlatform/EventPlatform.Api --launch-profile "https"`  
3. API https://localhost:7068  
4. Swagger https://localhost:7068/swagger/index.html  

## Описание API  
GET `/api/events` Получение списка всех мероприятий в базе  
GET `/api/events/{id:guid}` Получение подробной информации по выбранному мероприятию  
POST `/api/events` Добавление мероприятия в базу  
PUT `/api/events/{id:guid}` Обновление информации по мероприятию  
DELETE `/api/events/{id:guid}` Удаление мероприятия из базы  

Вывод структурирован моделью  

```json
{
  "data": object,
  "success": bool,
  "statusCode": int,
  "dateTime": datetime,
  "message": string
}
```  

 ## Changelog  
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