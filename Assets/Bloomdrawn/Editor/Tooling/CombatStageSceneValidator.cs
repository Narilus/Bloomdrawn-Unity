using System;
using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Bloomdrawn.Editor.Tooling
{
    public sealed class CombatStageSceneValidationSummary
    {
        public bool Valid { get; set; }
        public int MissingBehaviourCount { get; set; }
        public int CanvasCount { get; set; }
        public int EventSystemCount { get; set; }
        public int MainCameraCount { get; set; }
        public int FallbackActorCount { get; set; }
    }

    public static class CombatStageSceneValidator
    {
        public const string ScenePath = "Assets/Scenes/CombatStage.unity";

        public static CombatStageSceneValidationSummary ValidateCommittedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var missing = components.Count(component => component == null);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(camera => camera.CompareTag("MainCamera")).ToArray();
            var fallbackActors = UnityEngine.Object.FindObjectsByType<CombatActorFallbackView>(FindObjectsSortMode.None);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<CombatStageRuntimeBootstrap>();
            var drag = UnityEngine.Object.FindFirstObjectByType<CardDragLayer>();
            var valid = missing == 0 && canvases.Length == 1 && eventSystems.Length == 1 && cameras.Length == 1 && cameras[0].orthographic && fallbackActors.Length == 5 && bootstrap != null && drag != null && drag.PlayArea != null && drag.DragLayer != null;
            if (!valid) throw new InvalidOperationException("CombatStage validation failed: missing=" + missing + ", canvas=" + canvases.Length + ", eventSystem=" + eventSystems.Length + ", mainCamera=" + cameras.Length + ", fallbackActors=" + fallbackActors.Length + ".");
            return new CombatStageSceneValidationSummary { Valid = true, MissingBehaviourCount = missing, CanvasCount = canvases.Length, EventSystemCount = eventSystems.Length, MainCameraCount = cameras.Length, FallbackActorCount = fallbackActors.Length };
        }
    }
}
