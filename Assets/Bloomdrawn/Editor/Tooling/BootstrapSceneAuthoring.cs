using Bloomdrawn.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bloomdrawn.Editor.Tooling
{
    public static class BootstrapSceneAuthoring
    {
        [MenuItem("Bloomdrawn/Create Bootstrap Developer Shell")]
        public static void CreateOrUpdate()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var existing = Object.FindFirstObjectByType<BootstrapDevShell>();
            if (existing == null)
            {
                var canvasRoot = new GameObject("Bootstrap Developer Shell", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasRoot.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasRoot.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
                var shell = canvasRoot.AddComponent<BootstrapDevShell>();
                CreateText(canvasRoot.transform, "Title", "Bloomdrawn M0 Developer Shell", new Vector2(0f, 120f), 28);
                var reduced = CreateText(canvasRoot.transform, "Reduced Motion Status", string.Empty, new Vector2(0f, 55f), 20);
                var developer = CreateText(canvasRoot.transform, "Developer Status", string.Empty, new Vector2(0f, -10f), 16);
                var button = CreateButton(canvasRoot.transform, "Toggle Reduced Motion Seed", new Vector2(0f, -95f));
                button.onClick.AddListener(shell.ToggleReducedMotionSeed);
                shell.Configure(reduced, developer);
            }
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null) Object.DestroyImmediate(legacyModule);
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene);
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 position, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 50f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position)
        {
            var go = new GameObject("Toggle Reduced Motion Seed", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 48f);
            var text = CreateText(go.transform, "Label", label, Vector2.zero, 16);
            ((RectTransform)text.transform).sizeDelta = rect.sizeDelta;
            return go.GetComponent<Button>();
        }
    }
}
