using System.Reflection;

namespace EdgePilot.Core;

public static class ProductVersion
{
    public static string Value
    {
        get
        {
            var assembly = typeof(ProductVersion).Assembly;
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?
                .Split('+')[0];
            if (!string.IsNullOrWhiteSpace(informational)) return informational;

            return assembly.GetName().Version?.ToString() ?? "unknown";
        }
    }

    public static string Label => Value == "unknown" ? "Preview" : $"v{Value}";
}
