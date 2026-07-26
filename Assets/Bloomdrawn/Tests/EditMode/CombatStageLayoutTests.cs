using Bloomdrawn.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bloomdrawn.Tests.EditMode
{
    public sealed class CombatStageLayoutTests
    {
        [Test]
        public void CombatStage_HasIndependentActorsCanvasPolicyAndSeparatedSafeZones()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatStage.unity", OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(); var scaler = Object.FindFirstObjectByType<CanvasScaler>(); var layout = Object.FindFirstObjectByType<CombatStageLayout>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay)); Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize)); Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920,1080))); Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(.5f));
            Assert.That(Object.FindFirstObjectByType<PartyFormationView>().Actors, Has.Count.EqualTo(4)); Assert.That(Object.FindFirstObjectByType<EnemyFormationView>().Actors, Has.Count.EqualTo(1)); Assert.That(Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None), Has.Length.EqualTo(5));
            foreach(var actor in Object.FindObjectsByType<CombatActorView>(FindObjectsSortMode.None)) foreach(CombatActorAnchorRole role in System.Enum.GetValues(typeof(CombatActorAnchorRole))) Assert.That(actor.Anchor(role), Is.Not.Null);
            var hand = GameObject.Find("Hand Safe Area").GetComponent<RectTransform>(); var survival = GameObject.Find("Shared Survival Lane").GetComponent<RectTransform>(); var endTurn = GameObject.Find("End Turn Control").GetComponent<RectTransform>(); var enemyLane = GameObject.Find("Enemy Target Lane").GetComponent<RectTransform>();
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1)); Assert.That(hand.anchorMax.y, Is.LessThanOrEqualTo(survival.anchorMin.y)); Assert.That(endTurn.anchorMin.x, Is.GreaterThan(hand.anchorMax.x)); Assert.That(enemyLane.anchorMin.x, Is.GreaterThan(survival.anchorMax.x));
            foreach(var resolution in new[]{new Vector2(1920,1080),new Vector2(1920,1200),new Vector2(3440,1440)}) Assert.That(resolution.x/resolution.y, Is.GreaterThan(1.3f));
        }
    }
}
