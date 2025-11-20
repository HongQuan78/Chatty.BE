namespace Chatty.BE.Application.Interfaces.Services;

public interface IObjectMapper
{
    TDestination Map<TDestination>(object source);

    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
}
