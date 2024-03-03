using AutoMapper;

namespace Core.Extensions
{
    public static class ObjectExtensions
    {
        public static TDestination MapTo<TDestination>(this object source, IMapper mapper)
        {
            return mapper.Map<TDestination>(source);
        }
    }
}
