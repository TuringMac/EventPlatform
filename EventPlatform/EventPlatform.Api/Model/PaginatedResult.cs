namespace EventPlatform.Api.Model
{
    public class PaginatedResult<T>
    {
        public int TotalItems { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int PageItems { get; set; }
    }
}
