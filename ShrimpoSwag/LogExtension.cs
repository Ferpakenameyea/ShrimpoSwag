using Microsoft.CodeAnalysis;

namespace ShrimpoSwag
{
    internal static class LogExtension
    {
        public static void Log(this SourceProductionContext context, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "SG0001",
                title: "Source Generator Log",
                messageFormat: message,
                category: "SourceGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }
    }
}
