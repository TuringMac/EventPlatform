using System.Net;

namespace EventPlatform.Api;

// Класс ApiResult c возвращаемыми данными
// Наследуемся от базового класса с основными параметрами
public class ApiResult<T> : ApiBaseResult
{
    // Возвращаемые данные метода
    public required T Data { get; set; }
}

// Класс ApiResult без возвращаемых данных
// Наследуемся от базового класса с основными параметрами
public class ApiResult : ApiBaseResult { }

/// <summary>
/// Базовый класс с основными возвращаемыми параметрами
/// </summary>
public class ApiBaseResult
{
    /// <summary>
    /// Флаг, указывающий на успешность выполненного запроса
    /// </summary>
    public required bool Success { get; set; }
    /// <summary>
    /// Возвращаемый HTTP-код
    /// </summary>
    public required HttpStatusCode StatusCode { get; set; }
    /// <summary>
    /// Дата и время ответа
    /// </summary>
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Кастомное сообщение с дополнительной информацией
    /// Здесь может быть информация об ошибке в случае неуспеха
    /// </summary>
    public required string Message { get; set; }
}
