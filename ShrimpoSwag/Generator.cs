// #define DEBUG_SOURCE
// uncomment this to enable debug mode, the dotnet build command
// will freeze until you attach a debugger

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShrimpoSwag;


[Generator]
internal class CustomGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #if DEBUG_SOURCE
        if (!Debugger.IsAttached)
        {
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
            Debugger.Break();
        }
        #endif

        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetSemanticTarget(ctx))
            .Where(symbol => symbol is not null)
            .Collect();

        context.RegisterSourceOutput(classDeclarations, (spc, controllers) =>
        {
            foreach (var controllerInfo in controllers.OfType<ControllerInfo>())
            {
                var controllerSymbol = controllerInfo.ClassSymbol;
                var semanticModel = controllerInfo.Context.SemanticModel;
                List<(IMethodSymbol method, List<ControllerReturnType> returnTypes)> list = [];
                foreach (var method in controllerSymbol.GetMembers()
                             .OfType<IMethodSymbol>()
                             .Where(m =>
                                m.MethodKind == MethodKind.Ordinary &&
                                MethodUtil.IsPartialMethod(m)))
                {
                    var result = GetControllerMethodReturnTypes(method, semanticModel, spc);
                    list.Add((method, result));
                }

                var controllerNamespace = controllerSymbol.ContainingNamespace.ToDisplayString();
                var controllerName = controllerSymbol.Name;

                var source = new StringBuilder($$"""
                    using Microsoft.AspNetCore.Mvc;

                    {{NamespaceUtil.GetNameSpaceDeclaration(controllerNamespace)}}

                    public partial class {{controllerName}}
                    {
                    
                    """);

                foreach ((var method, var returnTypes) in list)
                {
                    var dtoSourceGenerator = new DtoSourceGenerator();
                    foreach (var returnType in returnTypes)
                    {
                        if (returnType.IsRawEnumerable)
                        {
                            source.AppendLine($"\t[ProducesResponseType({returnType.StatusCode}, Type = typeof({returnType.EnumerableTypeName}[]))]");
                        }
                        else
                        {
                            var dto = dtoSourceGenerator.GenerateDto(
                                returnType: returnType,
                                controllerName: controllerName,
                                methodName: method.Name,
                                @namespace: controllerNamespace
                            );

                            spc.AddDto(dto);

                            source.AppendLine($"\t[ProducesResponseType({returnType.StatusCode}, Type = typeof({dto.DtoClassName}))]");
                        }
                    }

                    source.Append('\t');
                    source.Append(MethodUtil.GetMethodSignature(method));
                    source.Append(";\n");

                    source.AppendLine();
                }

                source.AppendLine("}");

                spc.AddSource($"Generated_{controllerName}.g.cs",
                            SourceText.From(source.ToString(), Encoding.UTF8));
            }
        });
    }

    private static ControllerInfo? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;

        bool isPartial = classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            return null;
        }

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);
        if (classSymbol == null)
        {
            return null;
        }

        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ApiControllerAttribute")
            {
                return new ControllerInfo(
                    classSymbol, 
                    context);
            }
        }

        return null;
    }

    private static List<ControllerReturnType> GetControllerMethodReturnTypes(IMethodSymbol method, SemanticModel semanticModel, SourceProductionContext spc)
    {
        List<ControllerReturnType> result = [];

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
            {
                continue;
            }

            var returnStatements = methodSyntax.DescendantNodes()
                .OfType<ReturnStatementSyntax>();

            foreach (var returnStmt in returnStatements)
            {
                var expr = returnStmt.Expression;
                if (expr is InvocationExpressionSyntax invocation)
                {
                    string? methodName = MethodUtil.GetInvocationMethodName(invocation);
                    if (methodName == null)
                    {
                        continue;
                    }
                    if (!HttpInvocationMethods.IsHttpResponseMethod(methodName))
                    {
                        continue;
                    }

                    var type = GetActionResultMethodInvocationResultType(invocation, semanticModel, spc, methodName);
                    if (type != null)
                    {
                        result.Add(type);
                    }
                }
            }
        }

        return result;
    }

    private static ControllerReturnType? GetActionResultMethodInvocationResultType(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        SourceProductionContext spc,
        string methodName)
    {
        int statusCode = HttpInvocationMethods.GetStatusCode(methodName);
        var args = invocation.ArgumentList.Arguments;
        var result = new ControllerReturnType(statusCode);

        if (args.Count == 1)
        {
            var arg = args[0].Expression;
            var typeInfo = semanticModel.GetTypeInfo(arg);
            var type = typeInfo.Type;
            if (type == null)
            {
                spc.LogWarning($"Can't get type info of anolymous initialization {arg.GetText()}");
                return null;
            }

            if (EnumerableUtil.IsEnumerableType(type, out var elementType) && elementType != null)
            {
                if (elementType.IsAnonymousType)
                {
                    var generatedClass = new AnolymouseSourceGenerator(elementType).Generate(spc);
                    result.EnumerableTypeName = generatedClass.ClassName;
                }
                else
                {
                    result.EnumerableTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
            else
            {
                var propertyQuery = type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Select(member =>
                    {
                        var @namespace = member.ContainingNamespace.ToDisplayString();
                        string? extraUsing = null;
                        if (NamespaceUtil.NamespaceNeedsUsing(@namespace))
                        {
                            extraUsing = @namespace;
                        }

                        string typeName = member.Type.ToDisplayString();
                        if (member.Type.IsAnonymousType)
                        {
                            var generatedClass = new AnolymouseSourceGenerator(member.Type).Generate(spc);
                            typeName = generatedClass.ClassName;
                            extraUsing = null;
                        }
                        else if (EnumerableUtil.IsEnumerableType(type, out var elementType) && elementType != null)
                        {
                            // TODO: handle enumerable member
                        }

                        return new Property(
                                    typeName: typeName,
                                    name: member.MetadataName,
                                    extraUsing: extraUsing);
                    });
                result.Properties.AddRange(propertyQuery);
            }
        }

        return result;
    }
}

internal class ControllerInfo(INamedTypeSymbol classSymbol, GeneratorSyntaxContext context)
{
    public INamedTypeSymbol ClassSymbol { get; set; } = classSymbol;
    public GeneratorSyntaxContext Context { get; set; } = context;
}