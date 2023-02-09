using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Duffet;
using Duffet.Builders;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.Engines;

public class ResolveEngine
{
    private static readonly Regex QUERY_PATTERN = new("(?<query>[0-9a-zA-Z_]+)");
    private readonly IEnumerable<ResourceResolverBase> _resolvers;
    private readonly IEnumerable<ResolverTuple> _tuples;

    public ResolveEngine(IEnumerable<ResourceResolverBase> resolvers)
    {
        _resolvers = resolvers;
        // 保证 DomainName 为 null 最后被匹配到
        _tuples = _resolvers.SelectMany(GetTuplesInType).OrderByDescending(x => x.DomainName?.Length ?? -1);
    }

    private IEnumerable<ResolverTuple> GetTuplesInType(ResourceResolverBase resolver)
    {
        var res = resolver.GetType();
        var domain = res.GetCustomAttribute<ResourceDomainAttribute>();
        var domainName = domain?.DomainName;
        var type = res.GetCustomAttribute<ResourceTypeAttribute>();
        var methods = res.GetMethods();
        foreach (var method in methods)
            // 这不是 HyperaiX.UnitBase，对返回值严格要求
            if (method.IsPublic && (method.ReturnType == typeof(Result<ResolveResult, ResolveResultError>) ||
                                    method.ReturnType == typeof(Task<Result<ResolveResult, ResolveResultError>>)))
            {
                var methodType = method.GetCustomAttribute<ResourceTypeAttribute>();
                var expression = method.GetCustomAttribute<ResourceExpressionAttribute>();
                if (expression != null)
                {
                    var realType = methodType ?? type;
                    if (realType != null)
                        yield return new ResolverTuple(realType.TypeName, domainName, method, expression, resolver);
                }
            }
    }

    public async Task<Result<ResolveResult, ResolveResultError>> ResolveAsync(GameInstance instance, Uri resource)
    {
        if (resource.Scheme == "poly-res")
        {
            var type = resource.Host;
            var domain = !string.IsNullOrEmpty(resource.UserInfo) ? resource.UserInfo : null;
            var expression = resource.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            var query = HttpUtility.ParseQueryString(resource.Query);
            var resolver = _tuples.FirstOrDefault(x => type == x.TypeName && (domain == null
                ? x.DomainName == null
                : x.DomainName == domain || x.DomainName == null));
            if (resolver != null)
            {
                // prepare path arguments
                var builder = new BankBuilder();
                var match = resolver.Expression.Compiled.Match(expression);
                if (match.Success)
                {
                    foreach (Group group in match.Groups)
                    {
                        if (group.Success && group.Name != string.Empty)
                        {
                            var name = group.Name;
                            var value = group.Value;
                            builder.Property()
                                .Named(name)
                                .Typed(typeof(string))
                                .WithObject(value);
                        }
                    }
                }

                // prepare query arguments
                foreach (string key in query.Keys)
                {
                    builder.Property()
                        .Named(key)
                        .Typed(typeof(string))
                        .WithObject(query.Get(key));
                }

                var bank = builder.Build();
                var context = new ResolverContext(instance);
                resolver.Self.Context = context;
                return await ExecuteAsAsyncStateMachine(resolver.Method, resolver.Self, bank);
            }
            else
            {
                return Result<ResolveResult, ResolveResultError>.Err(ResolveResultError.NotFound);
            }
        }
        else
        {
            throw new ArgumentException("Scheme only accepts 'poly-res'", nameof(resource));
        }
    }

    private Task<Result<ResolveResult, ResolveResultError>> ExecuteAsAsyncStateMachine(MethodInfo method,
        object subject,
        Bank bank)
    {
        var arguments = bank.Serve(method);
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
        {
            return (Task<Result<ResolveResult, ResolveResultError>>)(method.Invoke(subject, arguments) ??
                                                                     Task.FromResult(
                                                                         Result<ResolveResult, ResolveResultError>.Err(
                                                                             ResolveResultError.Unknown)));
        }
        else
        {
            return Task.Run(() => method.Invoke(subject, arguments) as Result<ResolveResult, ResolveResultError> ??
                                  Result<ResolveResult, ResolveResultError>.Err(ResolveResultError.Unknown));
        }
    }

    private record ResolverTuple(string TypeName, string? DomainName, MethodInfo Method,
        ResourceExpressionAttribute Expression, ResourceResolverBase Self);
}