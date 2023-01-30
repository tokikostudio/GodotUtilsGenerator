using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Tokiko.SourceGenerators;

public static class BindExportPipeline
{
    private const string AttributeName  = "BindExportAttribute";
    private const string NodePathSuffix = "NodePath";
    private const string BoundTypeKey   = "BoundType";

    private class NodePathData
    {
        public string FullClassName  { get; set; }
        public string NodePathField  { get; set; }
        public string GeneratedField { get; set; }
        public string BoundTypeCast  { get; set; }
    }

    public static void Setup(IncrementalGeneratorInitializationContext context)
    {
        var attribute = @$"using System;
namespace {GodotUtilsGenerator.OutputNamespace};

[AttributeUsage(AttributeTargets.Field)]
public class {AttributeName} : Attribute
{{
    public {AttributeName}(Type type)
    {{
        {BoundTypeKey} = type;
    }}
    public Type {BoundTypeKey} {{ get; }}
}}";
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(AttributeName, SourceText.From(attribute, Encoding.UTF8)));

        var provider = context.SyntaxProvider
                              .CreateSyntaxProvider(predicate: static (s,   _) => SyntaxFilter(s),
                                                    transform: static (ctx, _) => SemanticFilter(ctx))
                              .Where(static v => v != null)
                              .Collect();

        var compilation = context.CompilationProvider.Combine(provider);
        context.RegisterSourceOutput(compilation, static (spc, source) => ExecuteDataGeneration(spc, source.Left, source.Right));
    }

    private static bool SyntaxFilter(SyntaxNode node) => (node is FieldDeclarationSyntax f && f.AttributeLists.Count > 0);

    private static FieldDeclarationSyntax SemanticFilter(GeneratorSyntaxContext context)
    {
        var fieldSyntax = (FieldDeclarationSyntax)context.Node;
        foreach (var nodeAttrListSyntax in fieldSyntax.AttributeLists)
        foreach (var nodeAttributeSyntax in nodeAttrListSyntax.Attributes)
        {
            var attributeInfo = context.SemanticModel.GetSymbolInfo(nodeAttributeSyntax);
            if (attributeInfo.Symbol is not IMethodSymbol attributeSymbol)
                continue;

            if (attributeSymbol.ContainingType.Name != AttributeName)
                continue;

            if (fieldSyntax.Parent is not ClassDeclarationSyntax)
                continue;

            return fieldSyntax;
        }

        // We didn't find the attribute we were looking for
        return null;
    }

    private static void ExecuteDataGeneration(SourceProductionContext                context,
                                              Compilation                            compilation,
                                              ImmutableArray<FieldDeclarationSyntax> nodePathFields)

    {
        var nodePathDataArray  = GetNodePathDataArray(compilation, nodePathFields, context.CancellationToken);
        var nodePathDataGroups = nodePathDataArray.GroupBy(ef => ef.FullClassName);

        foreach (var group in nodePathDataGroups)
        {
            var classFullName = group.Key;
            GenerateOutputSource(context, classFullName, group);
        }
    }

    private static List<NodePathData> GetNodePathDataArray(Compilation                            compilation,
                                                           ImmutableArray<FieldDeclarationSyntax> nodePathFields,
                                                           CancellationToken                      cancellation)
    {
        var nodePathData = new List<NodePathData>();
        foreach (var fieldSyntax in nodePathFields)
        {
            cancellation.ThrowIfCancellationRequested();
            var semanticModel = compilation.GetSemanticModel(fieldSyntax.SyntaxTree);
            foreach (var variableSyntax in fieldSyntax.Declaration.Variables)
            {
                var varSymbol = semanticModel.GetDeclaredSymbol(variableSyntax);
                var boundTypeCast = varSymbol.GetAttributes()
                                             .First(a => a.AttributeClass.Name == AttributeName)
                                             .ConstructorArguments[0]
                                             .Value.ToString();

                var nodePathField = varSymbol.Name;
                if (!nodePathField.EndsWith(NodePathSuffix))
                    continue;

                var subLength      = nodePathField.Length - NodePathSuffix.Length;
                var generatedField = nodePathField.Substring(0, subLength);

                nodePathData.Add(new NodePathData()
                {
                    FullClassName  = varSymbol.ContainingType.ToString(),
                    NodePathField  = nodePathField,
                    GeneratedField = generatedField,
                    BoundTypeCast  = boundTypeCast,
                });
            }
        }

        return nodePathData;
    }

    private static void GenerateOutputSource(SourceProductionContext   context,
                                             string                    fullClassName,
                                             IEnumerable<NodePathData> nodePathToGenerate)
    {
        var namespaceSplit = fullClassName.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);

        var classNamespace = namespaceSplit == -1
                                 ? string.Empty
                                 : fullClassName.Substring(0, namespaceSplit);
        var className = namespaceSplit == -1
                            ? fullClassName
                            : fullClassName.Substring(namespaceSplit + 1);

        var fieldDeclarations = new List<string>();
        var fieldBindings     = new List<string>();
        foreach (var data in nodePathToGenerate)
        {
            fieldDeclarations.Add($@"private {data.BoundTypeCast} {data.GeneratedField};");
            fieldBindings.Add($"{data.GeneratedField} = GetNode<{data.BoundTypeCast}>({data.NodePathField});");
        }

        var namespaceDeclaration = string.IsNullOrEmpty(classNamespace)
                                       ? string.Empty
                                       : $"namespace {classNamespace};";
        var source = $@"using Godot;
{namespaceDeclaration}
public partial class {className}
{{
{string.Join("\n", fieldDeclarations)}
private void BindExportedNodePaths()
{{
{string.Join("\n", fieldBindings)}
}}
}}";

        context.AddSource($"{fullClassName}.BindExport.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}