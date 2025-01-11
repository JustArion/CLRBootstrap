// namespace Dawn.AOT.Roslyn;
//
// using System.Diagnostics.CodeAnalysis;
// using System.Text;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Text;
//
// [Generator]
// public sealed class UnmanagedEntryPointGenerator : ISourceGenerator
// {
//     [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2000:Add analyzer diagnostic IDs to analyzer release")] 
//     private static readonly DiagnosticDescriptor MultipleUsageError = new(
//         id: "UEP001",
//         title: "UnmanagedEntryPoint attribute used multiple times",
//         messageFormat: "The [UnmanagedEntryPoint] attribute can only be used once per project",
//         category: "Usage",
//         DiagnosticSeverity.Error,
//         isEnabledByDefault: true);
//     
//     public void Initialize(GeneratorInitializationContext context)
//     {
//         context.RegisterForSyntaxNotifications(()=> new UnmanagedEntryPointSyntaxReceiver());
//     }
//
//     public void Execute(GeneratorExecutionContext context)
//     {
//         if (context.SyntaxContextReceiver is not UnmanagedEntryPointSyntaxReceiver receiver || receiver.CandidateMethods.Count == 0)
//             return; // This should never happen but we handle it just in-case.
//
//         // Our attribute must only be used once per project.
//         // if this is somehow bypassed, a compiler error will happen, which is fine. You can't have multiple UnmanagedCallersOnly of the same entrypoint       
//         if (receiver.CandidateMethods.Count != 1)
//         {
//             foreach (var location in receiver.CandidateMethods.Select(method => method.GetLocation())) 
//                 context.ReportDiagnostic(Diagnostic.Create(MultipleUsageError, location));
//             return;
//         }
//
//         var method = receiver.CandidateMethods[0];
//         var generatedSource = GenerateEntryPoint(method);
//         context.AddSource($"{method.Identifier.Text}.g.cs", SourceText.From(generatedSource, Encoding.Default));
//     }
//
//     private string GenerateEntryPoint(MethodDeclarationSyntax method)
//     {
//         var methodName = method.Identifier.Text;
//         var methodParameters = method.ParameterList.Parameters;
//
//         var generatedMethod = methodParameters.Count == 0
//             ? $$"""
//                 [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
//                 public static void Init()
//                 {
//                     {{methodName}}();
//                 }
//                 """
//             : $$"""
//                 [UnmanagedCallersOnly(EntryPoint = nameof(Init))]
//                 public static void Init(nint loaderInfo)
//                 {
//                     {{methodName}}(LoaderInformation.FromPointer(loaderInfo));
//                 }
//                 """;
//         
//         return $$"""    
//                  using System.Runtime.InteropServices;
//                  using Dawn.AOT;
//
//                  public static partial class EntryPointContainer
//                  {
//                      {{generatedMethod}}
//                  }
//                  """;
//     }
//
//     private class UnmanagedEntryPointSyntaxReceiver : ISyntaxContextReceiver
//     {
//         public List<MethodDeclarationSyntax> CandidateMethods { get; } = [];
//         
//         public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
//         {
//             if (context.Node is MethodDeclarationSyntax method 
//                 && method.AttributeLists
//                     .Any(x => 
//                         x.Attributes.Any(y => y.Name.ToString() == nameof(UnmanagedEntryPointAttribute))))
//                 CandidateMethods.Add(method);
//         }
//     }
// }