using System.Linq;
using Microsoft.CodeAnalysis;

internal static class EnumerableUtil
{
    public static bool IsEnumerableType(ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        if (IsEnumerable(typeSymbol))
        {
            elementType = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
            return true;
        }

        elementType = null;
        return false;
    }

    private static bool IsEnumerable(ITypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces
            .Any(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" || 
                      i.OriginalDefinition.ToDisplayString() == "System.Collections.IEnumerable");
    }
}