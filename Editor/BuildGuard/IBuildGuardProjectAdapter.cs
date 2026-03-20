namespace BuildGuard.Editor
{
    public interface IBuildGuardProjectAdapter
    {
        int Order { get; }
        void Validate(BuildGuardContext context);
        void Apply(BuildGuardContext context);
        void Restore(BuildGuardContext context);
    }
}
