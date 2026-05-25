using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SourceBase.Api.Shared;

namespace SourceBase.Api.Infrastructure.DbContexts;

public class ApplicationDbContextLoggingInterceptor(ILogger<ApplicationDbContextLoggingInterceptor> logger) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogCommand(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogCommand(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogCommand(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData);
        return ValueTask.FromResult(result);
    }

    private void LogCommand(DbCommand command, CommandExecutedEventData eventData)
    {
        var callSite = DbCommandCallSiteResolver.Resolve();
        var duration = eventData.Duration.TotalMilliseconds.ToString("0");
        var commandType = command.CommandType.ToString();

        logger.LogInformation(
            "{ClassName}.{MethodName}: Executed DbCommand ({Duration}ms) [Parameters=[{Parameters}], CommandType='{CommandType}', CommandTimeout='{CommandTimeout}']{NewLine}{CommandText}",
            callSite.ClassName,
            callSite.MethodName,
            duration,
            FormatParameters(command.Parameters),
            commandType,
            command.CommandTimeout,
            Environment.NewLine,
            command.CommandText);
    }

    private static string FormatParameters(DbParameterCollection parameters)
    {
        if (parameters.Count == 0)
            return string.Empty;

        return string.Join(", ", parameters.Cast<DbParameter>().Select(parameter => $"{parameter.ParameterName}='?' (DbType = {parameter.DbType})"));
    }
}

public record DbCommandCallSite(string ClassName, string MethodName);

public static class DbCommandCallSiteResolver
{
    private static readonly HashSet<Type> IgnoredTypes =
    [
        typeof(ApplicationDbContext),
        typeof(ApplicationDbContextAuditInterceptor),
        typeof(ApplicationDbContextHistoryInterceptor),
        typeof(ApplicationDbContextLoggingInterceptor),
        typeof(DbCommandCallSiteResolver),
        typeof(Utilities),
    ];

    public static DbCommandCallSite Resolve()
    {
        var frames = new StackTrace().GetFrames() ?? [];
        foreach (var frame in frames)
        {
            var callSite = TryResolve(frame.GetMethod());
            if (callSite is not null)
                return callSite;
        }

        return new DbCommandCallSite("UnknownCaller", "UnknownMethod");
    }

    private static DbCommandCallSite? TryResolve(MethodBase? method)
    {
        if (method?.DeclaringType is null)
            return null;

        var declaringType = method.DeclaringType;
        var methodName = method.Name;

        if (declaringType.IsNested && declaringType.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null)
        {
            methodName = ExtractGeneratedMethodName(declaringType.Name) ?? methodName;
            declaringType = declaringType.DeclaringType ?? declaringType;
        }

        var typeNamespace = declaringType.Namespace ?? string.Empty;
        if (typeNamespace.StartsWith("System", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("Microsoft", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("Serilog", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("Castle", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("DynamicClass", StringComparison.Ordinal))
        {
            return null;
        }

        if (typeNamespace.StartsWith("SourceBase.Api", StringComparison.Ordinal) is false)
            return null;

        if (IgnoredTypes.Contains(declaringType))
            return null;

        return new DbCommandCallSite(declaringType.Name, methodName);
    }

    private static string? ExtractGeneratedMethodName(string generatedTypeName)
    {
        var startIndex = generatedTypeName.IndexOf('<');
        var endIndex = generatedTypeName.IndexOf('>');
        if (startIndex < 0 || endIndex <= startIndex)
            return null;

        return generatedTypeName[(startIndex + 1)..endIndex];
    }
}