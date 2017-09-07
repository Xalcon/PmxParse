using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PmxParse.Tests
{
    [TestFixture]
    public class PmxFileTests
    {
        [Test]
        public void TestPmxHeader()
        {
            using (var stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Mockups", "Model.pmx")))
            {
                var model = new PmxModel(stream);
                Assert.AreEqual(2, model.Header.Version);

                Assert.IsNotNull(model.ModelInfo.CharacterName);
                Assert.IsNotNull(model.ModelInfo.UniversalCharacterName);
                Assert.IsNotNull(model.ModelInfo.Comment);
                Assert.IsNotNull(model.ModelInfo.UniversalComment);

                Assert.IsTrue(model.VertexData.Vertices.Length > 0);
                Assert.IsTrue(model.FaceData.Indices.All(i => i < model.VertexData.Vertices.Length));
            }
        }
    }
}
