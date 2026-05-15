#if UNITY_EDITOR
using LastLight.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastLight.Editor
{
    public static class LastLightProjectBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string OpeningAudioPath = "Assets/Audio/SFX/game-opening.wav";
        private const string MenuOptionAudioPath = "Assets/Audio/SFX/option-main-menu-choose.wav";

        [MenuItem("Last Light/Setup/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Bootstrap";

            var camera = new GameObject("Main Camera");
            var cameraComponent = camera.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = Color.black;
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 9f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var appRoot = new GameObject("LastLightApp");
            appRoot.AddComponent<LastLightApp>();

            var audioRoot = new GameObject("AudioManager");
            var audioSource = audioRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            var audioManager = audioRoot.AddComponent<AudioManager>();

            AssignImportedAudio(audioManager);
            CreateMenuScaffold();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AssignImportedAudio(AudioManager audioManager)
        {
            var serializedObject = new SerializedObject(audioManager);
            serializedObject.FindProperty("gameOpeningClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(OpeningAudioPath);
            serializedObject.FindProperty("menuOptionClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MenuOptionAudioPath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audioManager);
        }

        private static void CreateMenuScaffold()
        {
            var canvasObject = new GameObject("MainMenuCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var background = CreatePanel("Background", canvasObject.transform, new Color(0.07f, 0.07f, 0.08f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            StretchToFill(background.GetComponent<RectTransform>());

            CreateText(
                "Title",
                canvasObject.transform,
                "LAST LIGHT",
                124,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.72f),
                new Vector2(0f, 0f),
                new Vector2(760f, 180f));

            CreateModeCard(canvasObject.transform, "VerticalMode", "VERTICAL", "BEST 0", new Vector2(0f, 220f));
            CreateModeCard(canvasObject.transform, "HorizontalMode", "HORIZONTAL", "BEST 0", new Vector2(0f, -20f));

            CreateText(
                "Settings",
                canvasObject.transform,
                "SETTINGS",
                48,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.18f),
                new Vector2(0.5f, 0.18f),
                new Vector2(0f, 0f),
                new Vector2(360f, 90f));
        }

        private static void CreateModeCard(Transform parent, string name, string label, string score, Vector2 anchoredPosition)
        {
            var card = CreatePanel(name, parent, new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(720f, 180f));
            var outline = card.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, 3f);

            CreateText(
                $"{name}Label",
                card.transform,
                label,
                54,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.62f),
                new Vector2(0.5f, 0.62f),
                Vector2.zero,
                new Vector2(620f, 70f));

            CreateText(
                $"{name}Score",
                card.transform,
                score,
                38,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.3f),
                new Vector2(0.5f, 0.3f),
                Vector2.zero,
                new Vector2(320f, 60f));
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static void CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.28f);
            outline.effectDistance = new Vector2(2f, 2f);
        }

        private static void StretchToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
#endif
