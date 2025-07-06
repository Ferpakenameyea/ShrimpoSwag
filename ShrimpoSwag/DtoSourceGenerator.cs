using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ShrimpoSwag;

internal class DtoSourceGenerator
{
    private int _counter = 1;

    public DTO GenerateDto(
        ControllerReturnType returnType,
        string controllerName,
        string methodName,
        string @namespace)
    {
        var dtoSource = new StringBuilder();
        var dtoClassName = $"{controllerName}_{methodName}_DTO_{_counter}";
        _counter++;

        foreach (var @using in returnType.Properties
            .Select(x => x.ExtraUsing)
            .Where(@using => @using != null)
            .Distinct())
        {
            dtoSource.AppendLine($"using {@using};");
        }

        dtoSource.AppendLine();
        if (@namespace != "<global namespace>")
        {
            dtoSource.AppendLine(@namespace);
        }
        dtoSource.AppendLine();

        dtoSource.AppendLine($"public class {dtoClassName}");
        dtoSource.AppendLine("{");
        dtoSource.AppendLine("#pragma warning disable CS8618");

        foreach (var property in returnType.Properties)
        {
            dtoSource.AppendLine($"\tpublic {property.TypeName} {property.Name} {{ get; set; }}");
        }

        dtoSource.AppendLine("#pragma warning restore CS8618");
        dtoSource.AppendLine("}");

        return new DTO(
            source: dtoSource.ToString(),
            dtoClassName: dtoClassName
        );
    }
}

internal class DTO(string source, string dtoClassName)
{
    public string Source { get; set; } = source;
    public string DtoClassName { get; set; } = dtoClassName;
}

internal static class SourceProductionContextExtension
{
    public static void AddDto(this SourceProductionContext context, DTO dto)
    {
        context.AddSource(dto.DtoClassName, SourceText.From(dto.Source, Encoding.UTF8));
    }
}