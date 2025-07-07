using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

internal class AnonymousSourceGenerator(ITypeSymbol typeSymbol)
{
    private Guid id = Guid.NewGuid();
    private string ClassName => $"AnonymousClass_{id:N}";
    private string SourceFileName => $"{ClassName}.g.cs";
    private readonly ITypeSymbol typeSymbol = typeSymbol;

    public GeneratedAnonymousClassInfo Generate(SourceProductionContext spc)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"namespace {Constant.GeneratedClassNamespace};");
        builder.AppendLine();

        builder.AppendLine($"public class {ClassName}");
        builder.AppendLine("{");
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            string propertyName = property.MetadataName;
            string propertyType = property.Type.ToDisplayString();
            if (property.Type.IsAnonymousType)
            {
                var propertyInfo = new AnonymousSourceGenerator(property.Type).Generate(spc);
                propertyType = propertyInfo.ClassName;
            }

            builder.AppendLine($"\tpublic {propertyType} {propertyName} {{ get; set; }}");
        }

        builder.AppendLine("}");
        spc.AddSource(SourceFileName, SourceText.From(builder.ToString(), Encoding.UTF8));
        return new GeneratedAnonymousClassInfo(ClassName);
    }
}

internal class GeneratedAnonymousClassInfo(string className)
{
    public string ClassName { get; } = className;
}