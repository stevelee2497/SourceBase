using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Core.Extensions
{
    public static class LinqExtensions
    {
        public static IQueryable<TDestination> ProjectTo<TDestination>(this IQueryable source, IMapper mapper)
        {
            return source.ProjectTo<TDestination>(mapper.ConfigurationProvider);
        }
    }
}
