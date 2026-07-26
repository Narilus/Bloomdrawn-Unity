using System.Collections;
using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bloomdrawn.Tests.PlayMode
{
    public sealed class BootstrapSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapScene_EntersPlayMode()
        {
            Assert.That(PresentationAssemblyMarker.Name, Is.EqualTo("Bloomdrawn.Presentation"));

            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/Scenes/SampleScene.unity"));
            var shell = Object.FindFirstObjectByType<BootstrapDevShell>();
            Assert.That(shell, Is.Not.Null);
            var before = shell.ReducedMotionSeed;
            shell.ToggleReducedMotionSeed();
            Assert.That(shell.ReducedMotionSeed, Is.Not.EqualTo(before));
        }
    }
}
