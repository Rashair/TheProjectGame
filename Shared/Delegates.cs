namespace Shared
{
    public delegate TService ServiceResolver<TService>(string key)
        where TService : class;
}
