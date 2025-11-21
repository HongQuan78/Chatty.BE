using System.Linq;

namespace Chatty.BE.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var take = Math.Max(pageSize, 1);
        var skip = Math.Max(page - 1, 0) * take;
        return query.Skip(skip).Take(take);
    }
}
