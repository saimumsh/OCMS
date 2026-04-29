namespace OptimumCoaching.core.Core
{
    public class QueryResult<T>
    {
        public int TotalItems { get; set; }
        public IList<T> Items { get; set; } = new List<T>();
    }
}
