using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[Generator]
public sealed class UnmanagedEntryPointGenerator : IIncrementalGenerator
{
    private const string ATTRIBUTE_NAME = "EntryPoint";
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")] 
    private static readonly DiagnosticDescriptor MultipleUsageError = new(
        id: "UEP001",
        title: $"{ATTRIBUTE_NAME} attribute used multiple times",
        messageFormat: $"The [{ATTRIBUTE_NAME}] attribute can only be used once per project",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // These are all the method declarations with the attribute
        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsCandidateMethod,
                transform: GetMethodDeclarationSyntax)
            .Where(static method => method is not null);

        var collectedMethods = methodDeclarations
            .Collect();

        context.RegisterSourceOutput(collectedMethods, static (context, methods) =>
        {
            switch (methods.Length)
            {
                case 0:
                    return;
                case > 1:
                {
                    foreach (var method in methods.Where(x => x != null)) 
                        context.ReportDiagnostic(Diagnostic.Create(MultipleUsageError, method!.GetLocation()));

                    return;
                }
            }

            var methodToGenerate = methods[0]!;
            // var generatedSource = GenerateEntryPoint(methodToGenerate);

            var cu = CompilationUnit()
                .WithUsings(
                [
                    UsingDirective(ParseName("System.Runtime.InteropServices")),
                    UsingDirective(ParseName("Dawn.AOT"))
                ])
                .WithMembers(SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration("EntryPoint")
                        .AddModifiers(
                            Token(SyntaxKind.FileKeyword),
                            Token(SyntaxKind.StaticKeyword))

                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "Init")
                                .AddModifiers(
                                    Token(SyntaxKind.InternalKeyword), 
                                    Token(SyntaxKind.StaticKeyword))
                                .AddAttributeLists(
                                    AttributeList(SingletonSeparatedList(
                                    Attribute(ParseName("UnmanagedCallersOnly"))
                                        .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                                            AttributeArgument(
                                                NameEquals(IdentifierName("EntryPoint")), null,
                                                LiteralExpression(SyntaxKind.StringLiteralExpression,Literal("Init"))
                                            )
                                        )))
                                    ))
                                )
                                // nint loaderInfo
                                .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>([
                                    Parameter(Identifier("loaderInfo")).WithType(IdentifierName("nint"))
                                ])))
                                .WithBody(Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(GetFullyQualifiedMethodName(methodToGenerate)))
                                            .AddArgumentListArguments(
                                                methodToGenerate.ParameterList.Parameters
                                                    .Select(p => Argument(IdentifierName("LoaderInformation.FromPointer(loaderInfo)"))) // fix this
                                                    .ToArray()))))))))
                .NormalizeWhitespace();
            
            context.AddSource($"{GetFullyQualifiedMethodName(methodToGenerate)}.g.cs", SourceText.From(cu.ToFullString(), Encoding.UTF8));
        });


    }
    
    private static bool IsCandidateMethod(SyntaxNode node, CancellationToken _) =>
        node is MethodDeclarationSyntax method &&
        method.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == ATTRIBUTE_NAME));

    private static MethodDeclarationSyntax? GetMethodDeclarationSyntax(GeneratorSyntaxContext context, CancellationToken _)
    {
        return context.Node is MethodDeclarationSyntax method 
               && method.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr => attr.Name.ToString() == ATTRIBUTE_NAME)
            ? method
            : null;
    }
    
    private static string GenerateEntryPoint(MethodDeclarationSyntax method)
    {
        var absoluteMethodName = GetFullyQualifiedMethodName(method);
        var methodParameters = method.ParameterList.Parameters;

        var generatedMethod = methodParameters.Count == 0
            ? $$"""
                [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
                    public static void Init()
                    {
                        {{absoluteMethodName}}();
                    }
                """
            : $$"""
                [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
                    public static void Init(nint loaderInfo)
                    {
                        {{absoluteMethodName}}(LoaderInformation.FromPointer(loaderInfo));
                    }
                """;

        return $$"""
                 using global::System.Runtime.InteropServices;
                 using global::Dawn.AOT;

                 public static partial class EntryPointContainer
                 {
                     {{generatedMethod}}
                 }
                 """;
    }

    private static string GetFullyQualifiedMethodName(MethodDeclarationSyntax method)
    {
        // Get the containing type and namespace of the method
        var containingType = (method.Parent as TypeDeclarationSyntax)?.Identifier.Text;
        var namespaceDeclaration = method.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString();
        
        if (string.IsNullOrEmpty(namespaceDeclaration))
        {
            namespaceDeclaration = method.Ancestors()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault()?.Name.ToString();
        }

        if (string.IsNullOrEmpty(containingType) || string.IsNullOrEmpty(namespaceDeclaration))
            throw new InvalidOperationException("Method must be inside a namespace and a type");

        var fullyQualifiedMethodName = $"{namespaceDeclaration}.{containingType}.{method.Identifier.Text}";
        return fullyQualifiedMethodName;
    }
}
