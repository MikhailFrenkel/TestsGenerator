using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGenerator
{
    public class Generator
    {
        private readonly string _outputDirectory;
        private readonly int _writeCountFiles;
        private readonly int _readCountFiles;
        private readonly int _maxTasks;

        public Generator(string outputDirectory, int readCountFiles, int maxTasks, int writeCountFiles)
        {
            _outputDirectory = outputDirectory;
            _readCountFiles = readCountFiles;
            _maxTasks = maxTasks;
            _writeCountFiles = writeCountFiles;
        }

        public Task Generate(List<string> paths)
        {
            return Task.Run(() =>
            {
                var reader = new TransformBlock<string, Task<string>>(new Func<string, Task<string>>(ReadFile),
                    new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _readCountFiles});

                var generator = new TransformBlock<Task<string>, Task<List<GeneratedResult>>>(
                    new Func<Task<string>, Task<List<GeneratedResult>>>(GenerateFile),
                    new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _maxTasks});

                var writer = new ActionBlock<Task<List<GeneratedResult>>>(new Action<Task<List<GeneratedResult>>>(WriteFile),
                    new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _writeCountFiles});

                var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

                reader.LinkTo(generator, linkOptions);
                generator.LinkTo(writer, linkOptions);

                foreach (var path in paths)
                    reader.Post(path);

                reader.Complete();

                writer.Completion.Wait();
            });
        }

        private async Task<string> ReadFile(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return await sr.ReadToEndAsync();
            }
        }

        private async Task<List<GeneratedResult>> GenerateFile(Task<string> readSourceFile)
        {
            string source = await readSourceFile;
            var res = new List<GeneratedResult>();

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();

            var classes = compilationUnitSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classes)
            {
                var publicMethods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .Where(x => x.Modifiers.Any(y => y.ValueText == "public"));

                var ns = (classDeclaration.Parent as NamespaceDeclarationSyntax)?.Name.ToString();
                var className = classDeclaration.Identifier.ValueText;
                var methodsName = new List<string>();
                foreach (var method in publicMethods)
                {
                    var name = GetMethodName(methodsName, method.Identifier.ToString(), 0);
                    methodsName.Add(name);
                }

                NamespaceDeclarationSyntax namespaceDeclarationSyntax = NamespaceDeclaration(QualifiedName(
                    IdentifierName(ns), IdentifierName("Test")));

                CompilationUnitSyntax compilationUnit = CompilationUnit()
                    .WithUsings(GetUsings())
                    .WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceDeclarationSyntax
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration(className + "Tests")
                            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("TestClass"))))))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                            .WithMembers(GetMethods(methodsName))))));

                

                var outputPath = Path.Combine(_outputDirectory, className + "Test.cs");
                res.Add(new GeneratedResult
                {
                    Text = compilationUnit.NormalizeWhitespace().ToFullString(),
                    OutputPath = outputPath
                });
            }

            return res;
        }

        private async void WriteFile(Task<List<GeneratedResult>> generateResult)
        {
            var results = await generateResult;
            foreach (var result in results)
            {
                using (StreamWriter sw = new StreamWriter(result.OutputPath))
                {
                    await sw.WriteAsync(result.Text);
                }
            }
        }

        private SyntaxList<UsingDirectiveSyntax> GetUsings()
        {
            return new SyntaxList<UsingDirectiveSyntax>()
            {
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            QualifiedName(
                                IdentifierName("Microsoft"),
                                IdentifierName("VisualStudio")),
                            IdentifierName("TestTools")),
                        IdentifierName("UnitTesting")))
            };
        }

        private SyntaxList<MemberDeclarationSyntax> GetMethods(List<string> methods)
        {
            var result = new List<MemberDeclarationSyntax>();
            foreach (var method in methods) result.Add(GetMethod(method));

            return List(result);
        }

        private MethodDeclarationSyntax GetMethod(string name)
        {
            return MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),Identifier(name))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                                Attribute(IdentifierName("TestMethod"))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithBody(Block(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Assert"), IdentifierName("Fail"))))));
        }

        private string GetMethodName(List<string>methods, string method, int count)
        {
            var res = method + (count == 0 ? "" : count.ToString());
            if (methods.Contains(res)) return GetMethodName(methods, method, count + 1);

            return res;
        }
    }

    internal class GeneratedResult
    {
        public string Text { get; set; }
        public string OutputPath { get; set; }
    }
}
