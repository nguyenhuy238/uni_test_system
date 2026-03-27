using System.Linq.Expressions;

namespace UniTestSystem.Application.Interfaces;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
}

public sealed class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();
    private readonly List<string> _includeStrings = new();

    public Specification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    public Specification<T> Where(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
        return this;
    }

    public Specification<T> Include(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
        return this;
    }

    public Specification<T> Include(string includePath)
    {
        if (!string.IsNullOrWhiteSpace(includePath))
        {
            _includeStrings.Add(includePath);
        }
        return this;
    }
}
