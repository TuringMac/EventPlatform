# Event Platform  
## Запуск приложения  

1. `git clone -b sprint-1 https://github.com/TuringMac/EventPlatform`  
2. `dotnet run --project .\EventPlatform\EventPlatform\EventPlatform.Api --launch-profile "https"`  
3. API https://localhost:7068  
4. Swagger https://localhost:7068/swagger/index.html  

## Описание API  
GET `/events` Получение списка всех мероприятий в базе  
GET `/events/{id:guid}` Получение подробной информации по выбранному мероприятию  
POST `/events` Добавление мероприятия в базу  
PUT `/events/{id:guid}` Обновление информации по мероприятию  
DELETE `/events/{id:guid}` Удаление мероприятия из базы  

Вывод структурирован моделью  
```
{
  "data": object,
  "success": bool,
  "statusCode": int,
  "dateTime": datetime,
  "message": string
}
```  

 ## Changelog  
 - Добавлена вариативность в сервис
 - Используется DTO на границе контроллера и бизнес-логики
 - Добавлен Swagger
 - Объявлены зависимости с указанием срока жизни
 - Реализован эндпоинт со структурированным ответом
 - Реализованы интерфейсы
 - Разработаны интерфейсы бизнес-логики и нфраструктуры
 - Пустой проект  