using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestsGenerator.Tests
{
    [TestClass]
    public class GeneratorTests
    {
        //todo: с помощью roslyn проверить синтаксис
        private string _outputDirectory = "../../../TestGeneratedClassesResults";
        private Generator _generator;
        private List<string> _paths;

        [TestInitialize]
        public void Initialize()
        {
            _generator = new Generator(_outputDirectory, 2, 2, 2);
            _paths = new List<string>()
            {
                "../../../TestClasses/Blowfish.cs",
                "../../../TestClasses/DoubleGenerator.cs"
            };
            _generator.Generate(_paths);
        }

        [TestMethod]
        public void MsTestUsing()
        {
            var expected = "using Microsoft.VisualStudio.TestTools.UnitTesting;";
            Assert.IsTrue(CheckContains(expected));
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
            var expected = "[TestClass]";
            Assert.IsTrue(CheckContains(expected));
        }

        [TestMethod]
        public void CheckRepeatedMethods()
        {
            var expected = 1;
            var method = "EncipherTest";
            var file = File.ReadAllText(_outputDirectory + "/BlowfishTest.cs");
            var methodsCount = file.Split(' ').Count(x => x.Contains(method));

            Assert.AreEqual(methodsCount, expected);
        }

        private bool CheckContains(string expected)
        {
            var files = Directory.GetFiles(_outputDirectory);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                if (!text.Contains(expected))
                {
                    return false;
                }
            }

            return true;
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
