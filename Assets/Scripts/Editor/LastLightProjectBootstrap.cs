#if UNITY_EDITOR
using LastLight.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
    }
}
#endif
