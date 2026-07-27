using System.Collections.Generic;
using System.Linq;
using Bloomdrawn.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bloomdrawn.Editor.Tooling
{
    /// <summary>Unity-aware authoring for the committed generic M1 combat scene. It contains no gameplay definitions.</summary>
    public static class CombatStageAuthoring
    {
        [MenuItem("Bloomdrawn/Create M1 Combat Stage")]
        public static void CreateOrUpdate()
        {
            M1RuntimeFixtureArtifactGenerator.Generate();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Combat Stage", typeof(CombatStageLayout), typeof(CombatPresentationController), typeof(CombatStageRuntimeBootstrap));
            CreateCamera(root.transform);

            var canvas = new GameObject("Combat Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(root.transform, false);
            var unityCanvas = canvas.GetComponent<Canvas>(); unityCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(CombatStageLayout.ReferenceWidth, CombatStageLayout.ReferenceHeight); scaler.matchWidthOrHeight = CombatStageLayout.WidthHeightMatch;
            CreatePanel(canvas.transform, "Background Decor", Vector2.zero, Vector2.one, new Color(.035f, .055f, .11f, 1f), false);

            var party = new GameObject("Party Formation", typeof(PartyFormationView)); party.transform.SetParent(canvas.transform, false);
            var enemies = new GameObject("Enemy Formation", typeof(EnemyFormationView)); enemies.transform.SetParent(canvas.transform, false);
            var partyActors = CreateActors(party.transform, "party", 4).ToArray();
            var enemyActors = CreateActors(enemies.transform, "enemy", 1).ToArray();
            party.GetComponent<PartyFormationView>().Configure(partyActors.Select(actor => actor.Actor).ToList());
            enemies.GetComponent<EnemyFormationView>().Configure(enemyActors.Select(actor => actor.Actor).ToList());
            root.GetComponent<CombatPresentationController>().Configure(party.GetComponent<PartyFormationView>(), enemies.GetComponent<EnemyFormationView>());

            var survival = Region(canvas.transform, "Shared Survival Lane", new Vector2(.025f, .82f), new Vector2(.42f, .96f));
            var mana = Region(canvas.transform, "Mana Region", new Vector2(.025f, .69f), new Vector2(.25f, .80f));
            var phase = Region(canvas.transform, "Phase Region", new Vector2(.42f, .88f), new Vector2(.72f, .96f));
            var hand = Region(canvas.transform, "Hand Safe Area", new Vector2(.20f, .02f), new Vector2(.80f, .28f));
            var endTurn = Region(canvas.transform, "End Turn Control", new Vector2(.82f, .07f), new Vector2(.97f, .18f));
            var log = Region(canvas.transform, "Combat Log Overlay", new Vector2(.72f, .72f), new Vector2(.97f, .94f));
            var enemyLane = Region(canvas.transform, "Enemy Target Lane", new Vector2(.58f, .25f), new Vector2(.96f, .70f));
            var play = Region(canvas.transform, "Play Area", new Vector2(.25f, .29f), new Vector2(.75f, .53f));
            CreatePanel(play, "Play Area Backdrop", Vector2.zero, Vector2.one, new Color(.13f, .24f, .30f, .36f), false);
            var drag = Region(canvas.transform, "Card Drag Layer", Vector2.zero, Vector2.one);
            var dragLayer = drag.gameObject.AddComponent<CardDragLayer>(); dragLayer.Configure(play, drag);

            var survivalText = CreateText(survival, "Survival Text", 31, TextAlignmentOptions.Left);
            var manaText = CreateText(mana, "Mana Text", 28, TextAlignmentOptions.Left);
            var phaseText = CreateText(phase, "Phase Text", 25, TextAlignmentOptions.Center);
            var logText = CreateText(log, "Combat Log Text", 22, TextAlignmentOptions.TopLeft);
            var endButton = CreateButton(endTurn, "End Turn", "End Turn");
            var hud = canvas.AddComponent<CombatHudView>();
            hud.Configure(hand, survivalText, manaText, phaseText, logText, endButton, partyActors.Concat(enemyActors).ToArray(), enemyActors.Select(actor => actor.Target).ToArray());

            root.GetComponent<CombatStageLayout>().Configure(hand, survival, enemyLane, endTurn, log);
            root.GetComponent<CombatStageRuntimeBootstrap>().Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(M1RuntimeFixtureArtifactGenerator.ArtifactPath), root.GetComponent<CombatPresentationController>(), hud, dragLayer);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatStage.unity");
        }

        private static void CreateCamera(Transform parent)
        {
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera"; camera.transform.SetParent(parent, false);
            var value = camera.GetComponent<Camera>(); value.orthographic = true; value.orthographicSize = 5; value.clearFlags = CameraClearFlags.SolidColor; value.backgroundColor = new Color(.035f, .055f, .11f, 1f);
        }

        private static RectTransform Region(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, float size, TextAlignmentOptions alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); text.transform.SetParent(parent, false);
            var rect = (RectTransform)text.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(12, 8); rect.offsetMax = new Vector2(-12, -8);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (text.font == null) throw new System.InvalidOperationException("The project TMP fallback font is unavailable.");
            text.fontSize = size; text.alignment = alignment; text.color = Color.white; text.raycastTarget = false; return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(.24f, .50f, .67f, 1f);
            var text = CreateText(root.transform, "Label", 28, TextAlignmentOptions.Center); text.text = label;
            return root.GetComponent<Button>();
        }

        private static IEnumerable<CombatActorFallbackView> CreateActors(Transform parent, string role, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var actor = new GameObject(role + " Actor " + i, typeof(RectTransform), typeof(CombatActorView), typeof(CombatActorTokenReaction), typeof(CombatActorFallbackView)); actor.transform.SetParent(parent, false);
                var rect = (RectTransform)actor.transform; var x = role == "party" ? .10f + i * .105f : .73f + i * .12f; rect.anchorMin = rect.anchorMax = new Vector2(x, .48f); rect.sizeDelta = new Vector2(150, 250);
                Transform Anchor(string name) { var anchor = new GameObject(name, typeof(RectTransform)); anchor.transform.SetParent(actor.transform, false); var anchorRect = (RectTransform)anchor.transform; anchorRect.anchorMin = Vector2.zero; anchorRect.anchorMax = Vector2.one; anchorRect.offsetMin = anchorRect.offsetMax = Vector2.zero; return anchor.transform; }
                var visual = Anchor("Visual Anchor"); var targetAnchor = Anchor("Target Anchor"); var selection = Anchor("Selection Anchor"); var status = Anchor("Status Anchor"); var vfx = Anchor("VFX Anchor"); var intent = Anchor("Intent Anchor");
                var actorView = actor.GetComponent<CombatActorView>(); actorView.Configure("fixture." + role + "." + i, visual, targetAnchor, selection, status, vfx, intent);
                var visualImage = CreatePanel(visual, "Fallback Visual", Vector2.zero, Vector2.one, role == "enemy" ? new Color(.7f, .18f, .22f, 1f) : new Color(.18f, .42f, .7f, 1f), false);
                var label = CreateText(visual, "Fallback Label", 18, TextAlignmentOptions.Center);
                CombatEnemyTargetView target = null;
                if (role == "enemy") { var targetImage = CreatePanel(targetAnchor, "Target Affordance", Vector2.zero, Vector2.one, new Color(.7f, .18f, .22f, .72f), true); target = targetAnchor.gameObject.AddComponent<CombatEnemyTargetView>(); target.Configure(actorView, targetImage); }
                var fallback = actor.GetComponent<CombatActorFallbackView>(); fallback.Configure(actorView, role == "enemy", visualImage, label, target);
                yield return fallback;
            }
        }
    }
}
