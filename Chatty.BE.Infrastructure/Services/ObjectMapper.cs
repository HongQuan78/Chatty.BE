using AutoMapper;
using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Infrastructure.Services;

public sealed class ObjectMapper(IMapper mapper) : IObjectMapper
{
    private readonly IMapper _mapper = mapper;

    public TDestination Map<TDestination>(object source)
    {
        return _mapper.Map<TDestination>(source);
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        return _mapper.Map(source, destination);
    }
}
