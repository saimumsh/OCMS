namespace OptimumCoaching.core.Core
{
    public abstract class Entity
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        protected Entity()
        {
            Id = Guid.NewGuid();
            IsActive = true;
            IsDeleted = false;
        }
    }
}
