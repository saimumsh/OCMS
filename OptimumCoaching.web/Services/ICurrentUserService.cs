namespace OptimumCoaching.web.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string Name { get; }
        bool IsAuthenticated { get; }
    }
}
