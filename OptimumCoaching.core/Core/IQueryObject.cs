namespace OptimumCoaching.core.Core
{
    public abstract class IQueryObject
    {
        public string? SortBy { get; set; }
        public bool IsSortAscending { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        protected IQueryObject()
        {
            Page = 1;
            PageSize = 50;
        }
    }
}
