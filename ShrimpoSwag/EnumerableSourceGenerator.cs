using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

internal class EnumerableSourceGenerator
{
    public GeneratedEnumerableClassInfo? ClassInfo { get; private set; } = null;
    public bool Cached => ClassInfo is not null;
    private readonly ITypeSymbol _elementType;
    private Guid id = Guid.NewGuid();
    public string ClassName => $"EnumerableSource_{id:N}";

    private EnumerableSourceGenerator(ITypeSymbol elementType)
    {
        _elementType = elementType;
    }

    public GeneratedEnumerableClassInfo Generate(SourceProductionContext spc)
    {
        if (Cached)
        {
            return ClassInfo!;
        }
        
        var builder = new StringBuilder();
        builder.AppendLine($"public class {ClassName} : IEnumerable<{_elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>");
        builder.AppendLine("{");
        builder.AppendLine($"\tpublic IEnumerator<{_elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> GetEnumerator() => throw new NotImplementedException();");
        builder.AppendLine("\tIEnumerator IEnumerable.GetEnumerator() => GetEnumerator();");
        builder.AppendLine("}");
        spc.AddSource($"{ClassName}.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        ClassInfo = new GeneratedEnumerableClassInfo(ClassName);
        return ClassInfo;
    }

    private static readonly Dictionary<string, EnumerableSourceGenerator> _cache = [];

    public static EnumerableSourceGenerator FromType(ITypeSymbol symbol)
    {
        if (!EnumerableUtil.IsEnumerableType(symbol, out var elementType))
        {
            throw new ArgumentException("Symbol is not enumerable");
        }

        if (elementType is null)
        {
            throw new ArgumentException("Enumerate target type is null");
        }

        if (_cache.TryGetValue(symbol.ToDisplayString(), out var generator))
        {
            return generator;
        }

        generator = new EnumerableSourceGenerator(elementType);
        _cache.Add(symbol.ToDisplayString(), generator);
        return generator;
    }
}

internal class GeneratedEnumerableClassInfo(string className)
{
    public string ClassName { get; } = className;

}