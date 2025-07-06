using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class MethodUtil
{
    public static bool IsPartialMethod(IMethodSymbol methodSymbol)
    {
        foreach (var syntaxRef in methodSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is MethodDeclarationSyntax methodSyntax)
            {
                if (methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static string GetMethodSignature(IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters
            .Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}")
            .ToArray();

        var parameterString = string.Join(", ", parameters);

        var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return $"public partial {returnType} {methodSymbol.Name}({parameterString})";
    }

    public static string? GetInvocationMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            _ => null
        };
    }
}