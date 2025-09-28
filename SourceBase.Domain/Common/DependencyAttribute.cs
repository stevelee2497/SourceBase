using Microsoft.Extensions.DependencyInjection;

namespace SourceBase.Domain.Common;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class DependencyAttribute(ServiceLifetime serviceLifeTime, Type type) : Attribute
{
    public ServiceLifetime ServiceLifeTime { get; } = serviceLifeTime;

    public Type ServiceType { get; } = type;
}

public class TransientDependencyAttribute<T>() : DependencyAttribute(ServiceLifetime.Transient, typeof(T));

public class ScopedDependencyAttribute<T>() : DependencyAttribute(ServiceLifetime.Scoped, typeof(T));

public class SingletonDependencyAttribute<T>() : DependencyAttribute(ServiceLifetime.Singleton, typeof(T));