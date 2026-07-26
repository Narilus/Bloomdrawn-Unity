using System.Collections.Generic;
using Bloomdrawn.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bloomdrawn.Editor.Tooling
{
    public static class CombatStageAuthoring
    {
        [MenuItem("Bloomdrawn/Create M1 Combat Stage")]
        public static void CreateOrUpdate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Combat Stage", typeof(CombatStageLayout)); root.AddComponent<CombatPresentationController>();
            var canvas = new GameObject("Combat Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas.transform.SetParent(root.transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var party = new GameObject("Party Formation", typeof(PartyFormationView)); party.transform.SetParent(canvas.transform); var enemies = new GameObject("Enemy Formation", typeof(EnemyFormationView)); enemies.transform.SetParent(canvas.transform);
            party.GetComponent<PartyFormationView>().Configure(CreateActors(party.transform, "party", 4)); enemies.GetComponent<EnemyFormationView>().Configure(CreateActors(enemies.transform, "enemy", 1)); root.GetComponent<CombatPresentationController>().Configure(party.GetComponent<PartyFormationView>(), enemies.GetComponent<EnemyFormationView>());
            var survival=Region(canvas.transform,"Shared Survival Lane",new Vector2(.18f,.25f),new Vector2(.42f,.36f)); Region(canvas.transform,"Mana Region",new Vector2(.03f,.05f),new Vector2(.18f,.16f)); var hand=Region(canvas.transform,"Hand Safe Area",new Vector2(.25f,.01f),new Vector2(.72f,.25f)); var endTurn=Region(canvas.transform,"End Turn Control",new Vector2(.78f,.06f),new Vector2(.94f,.16f)); var log=Region(canvas.transform,"Combat Log Overlay",new Vector2(.72f,.68f),new Vector2(.96f,.93f)); var enemyLane=Region(canvas.transform,"Enemy Target Lane",new Vector2(.62f,.25f),new Vector2(.96f,.68f)); var play=Region(canvas.transform,"Play Area",new Vector2(.25f,.25f),new Vector2(.72f,.48f)); var drag=Region(canvas.transform,"Card Drag Layer",Vector2.zero,Vector2.one); drag.gameObject.AddComponent<CardDragLayer>().Configure(play,drag); root.GetComponent<CombatStageLayout>().Configure(hand,survival,enemyLane,endTurn,log);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatStage.unity");
        }
        private static RectTransform Region(Transform parent,string name,Vector2 min,Vector2 max){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent);var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
        private static List<CombatActorView> CreateActors(Transform parent,string role,int count)
        { var result=new List<CombatActorView>(); for(var i=0;i<count;i++){var actor=new GameObject(role+" Actor "+i,typeof(RectTransform));actor.AddComponent<CombatActorView>();actor.AddComponent<CombatActorTokenReaction>();actor.transform.SetParent(parent);var rect=(RectTransform)actor.transform;var x=role=="party"?.12f+i*.10f:.72f+i*.12f;rect.anchorMin=rect.anchorMax=new Vector2(x,.48f);rect.sizeDelta=new Vector2(150,250); Transform A(string n){var a=new GameObject(n,typeof(RectTransform));a.transform.SetParent(actor.transform);return a.transform;} var view=actor.GetComponent<CombatActorView>();view.Configure("fixture."+role+"."+i,A("Visual Anchor"),A("Target Anchor"),A("Selection Anchor"),A("Status Anchor"),A("VFX Anchor"),A("Intent Anchor"));result.Add(view);}return result; }
    }
}
