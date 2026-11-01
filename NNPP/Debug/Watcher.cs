using System.Linq.Expressions;

namespace NNPP.Debug;

public class Watcher
{
    private Dictionary<string, Func<object?>> Watches { get; } = new();

    public void Watch<T>(Expression<Func<T>> expr)
    {
        var f = expr.Compile();
        Watches[GetName(expr.Body)] = () => f();
    }

    public void Watch(string name, Func<object?> func)
    {
        Watches[name] = func;
    }
    
    public void Watch(string name, object? o)
    {
        Watches[name] = () => o;
    }

    public Dictionary<string, string> GetWatchedValues()
    {
        return Watches.Select(q => new
            {
                q.Key,
                Value = Pretty(q.Value()),
            })
            .ToDictionary(q => q.Key, q => q.Value);
    }

    private static string Pretty(object? o)
    {
        if (o is null)
        {
            return "NULL";
        }

        switch (o)
        {
            case string s:
                return $"\"{s}\"";
            case float f:
                return ($"{f:F5}").TrimEnd('0').TrimEnd('.');
            case double f:
                return ($"{f:F5}").TrimEnd('0').TrimEnd('.');
            default:
                return o.ToString() ?? "NULL";
        }
    }

    private static string GetName(Expression expr)
    {
        var stack = new Stack<string>();

        while (true)
        {
            switch (expr)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert } un:
                    expr = un.Operand;
                    continue;
                case MemberExpression member:
                    stack.Push(member.Member.Name);

                    switch (member.Expression)
                    {
                        case null:
                            stack.Push(member.Member.DeclaringType!.Name);
                            return string.Join(".", stack);
                        case ConstantExpression:
                            return string.Join(".", stack);
                        default:
                            expr = member.Expression;
                            continue;
                    }

                default:
                    return expr.ToString();
            }
        }
    }
}