using System.Linq;
using Bloomdrawn.Application;
using Bloomdrawn.Content;
using Bloomdrawn.Engine;
using NUnit.Framework;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class AssemblyBoundarySmokeTests
    {
        [Test]
        public void PureAssemblies_DoNotReferenceUnityOrEditorAssemblies()
        {
            Assert.That(EngineAssemblyMarker.Name, Is.EqualTo("Bloomdrawn.Engine"));
            Assert.That(ContentAssemblyMarker.Name, Is.EqualTo("Bloomdrawn.Content"));
            Assert.That(ApplicationAssemblyMarker.Name, Is.EqualTo("Bloomdrawn.Application"));

            AssertPureAssembly(typeof(EngineAssemblyMarker).Assembly);
            AssertPureAssembly(typeof(ContentAssemblyMarker).Assembly);
            AssertPureAssembly(typeof(ApplicationAssemblyMarker).Assembly);
        }

        private static void AssertPureAssembly(System.Reflection.Assembly assembly)
        {
            var unityReference = assembly.GetReferencedAssemblies()
                .FirstOrDefault(reference => reference.Name.StartsWith("Unity", System.StringComparison.Ordinal));

            Assert.That(unityReference, Is.Null, string.Format("{0} references {1}.", assembly.GetName().Name, unityReference));
        }
    }
}
