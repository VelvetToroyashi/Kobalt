namespace Kobalt.Infrastructure;

/// <summary>
/// Indicates that the class or method referenced should not be automatically registered by assembly-scanning methods.
/// </summary>
[AttributeUsage((AttributeTargets.Class | AttributeTargets.Method))]
public class SkipAssemblyDiscoveryAttribute : Attribute;