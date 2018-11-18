using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGenerator.Tests
{
    [TestClass]
    public class GeneratorTest
    {
        //todo: с помощью roslyn проверить синтаксис
        private string _outputDirectory = "../../../TestGeneratedClassesResults";
        private Generator _generator;
        private List<string> _paths;
        private CompilationUnitSyntax _compilationUnit;

        [TestInitialize]
        public void Initialize()
        {
            _generator = new Generator(_outputDirectory, 2, 2, 2);
            _paths = new List<string>()
            {
                "../../../TestClasses/Blowfish.cs"
            };
            //Wait??? (не ожидает выполнения?)
            _generator.Generate(_paths).Wait();
            //Exception: file used by another process.
            Thread.Sleep(2000);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(Path.Combine(_outputDirectory, "BlowfishTest.cs")));
            _compilationUnit = syntaxTree.GetCompilationUnitRoot();
        }

        [TestMethod]
        public void MsTestUsing()
        {
            var str = "Microsoft.VisualStudio.TestTools.UnitTesting";
            var actual = _compilationUnit.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Count(x => x.Name.ToString() == str);
            Assert.AreNotEqual(0, actual);
        }

        [TestMethod]
        public void GeneratedFiles()
        {
            var files = Directory.GetFiles(_outputDirectory);

            Assert.AreNotEqual(0, files.Length);
        }

        [TestMethod]
        public void CheckAttributeTestClass()
        {
            var attribute = "TestClass";
            var actual = _compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .All(x => x.AttributeLists.Any(y => y.Attributes.
                    Any(z => z.ToString() == attribute)));
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void CheckRepeatedMethods()
        {
            var expected = 1;
            var method = "EncipherTest";
            var actual = _compilationUnit.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Count(x => x.Identifier.ToString().Contains(method));
            Assert.AreEqual(expected, actual);
        }

        [TestCleanup]
        public void CleanUp()
        {
            var files = Directory.GetFiles(_outputDirectory);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}

