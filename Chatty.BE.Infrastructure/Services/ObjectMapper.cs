using AutoMapper;
using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Infrastructure.Services;

public sealed class ObjectMapper(IMapper mapper) : IObjectMapper
{
    public TDestination Map<TDestination>(object source)
    {
        return mapper.Map<TDestination>(source);
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        return mapper.Map(source, destination);
    }
}
