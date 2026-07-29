using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace StockScanTool.Tests.Unit;

internal static class AsyncQueryableHelper
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
        => new AsyncQueryable<T>(source);

    internal class AsyncQueryable<T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>, IAsyncQueryProvider
    {
        private readonly IEnumerable<T> _source;

        public AsyncQueryable(IEnumerable<T> source)
        {
            _source = source;
            InnerQueryable = source.AsQueryable();
        }

        private IQueryable<T> InnerQueryable { get; }

        public Type ElementType => typeof(T);
        public Expression Expression => InnerQueryable.Expression;
        public IQueryProvider Provider => this;

        public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _source.GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumerator<T>(_source.GetEnumerator());

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult);

            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0];
                var method = typeof(AsyncQueryable<T>)
                    .GetMethod(nameof(ExecuteTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(innerType);
                return (TResult)method.Invoke(this, [expression])!;
            }

            return ExecuteSync<TResult>(expression);
        }

        private TResult ExecuteSync<TResult>(Expression expression)
        {
            var func = Expression.Lambda<Func<T[], TResult>>(
                expression,
                Expression.Parameter(typeof(T[]), "_"));

            return func.Compile().Invoke(_source.ToArray());
        }

        private Task<TInner> ExecuteTaskAsync<TInner>(Expression expression)
        {
            var data = _source.ToArray();
            var func = Expression.Lambda<Func<T[], TInner>>(
                expression,
                Expression.Parameter(typeof(T[]), "_"));

            var result = func.Compile().Invoke(data);
            return Task.FromResult(result);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new AsyncQueryable<TElement>(
                InnerQueryable.Provider.CreateQuery<TElement>(expression).AsEnumerable());

        public IQueryable CreateQuery(Expression expression)
            => InnerQueryable.Provider.CreateQuery(expression);

        public TResult Execute<TResult>(Expression expression)
            => InnerQueryable.Provider.Execute<TResult>(expression);

        public object? Execute(Expression expression)
            => InnerQueryable.Provider.Execute(expression);
    }

    internal class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public T Current => inner.Current;
    }
}
