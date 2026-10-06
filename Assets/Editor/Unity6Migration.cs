using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CastleJourney.Editor
{
    public static class Unity6Migration
    {
        public static readonly string[] ScenePaths =
        {
            "Assets/Scenes/UI/MainMenu.unity",
            "Assets/Scenes/World1.unity",
            "Assets/Scenes/World2.unity",
            "Assets/Scenes/World3.unity",
            "Assets/Scenes/UI/EndGame.unity"
        };

        [MenuItem("Castle Journey/Validate Project")]
        public static void Validate()
        {
            RequireSavedScenes();
            var actualScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled).Select(scene => scene.path);
            if (!actualScenes.SequenceEqual(ScenePaths))
                throw new BuildFailedException("Build scene order changed; gameplay uses numeric scene indices.");

            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new BuildFailedException("Assign the Castle Journey URP asset in Graphics Settings.");

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
            if (package == null || package.version != "6.6.0")
                throw new BuildFailedException("Cinemachine must resolve to the editor's supported 6.6.0 version.");
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            if (settings.FindProperty("activeInputHandler").intValue != 1)
                throw new BuildFailedException("Use Input System Package (New), not the legacy or Both backends.");

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in ScenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        {
                            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                                throw new BuildFailedException($"Missing script: {path}/{transform.name}");
                        }
                    }
                    Debug.Log($"Validated scene: {path}");
                }
            }
            finally
            {
                RestoreScenes(setup);
            }
            Debug.Log("Castle Journey Unity 6 validation passed.");
        }

        [MenuItem("Castle Journey/Upgrade Asset Serialization")]
        public static void UpgradeSerialization()
        {
            RequireSavedScenes();
            if (!Application.unityVersion.StartsWith("6000.6."))
                throw new InvalidOperationException("Run this migration with Unity 6000.6.");

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // The original keystore lived on a Windows drive and was never part of this repository.
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";

            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null)
                input.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();

            RepairStaleAttackTriggers();
            // Serialize through Unity so importer flags, sprite IDs and GUIDs are retained.
            var paths = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)).ToArray();
            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            AssetDatabase.SaveAssets();
            Debug.Log("Castle Journey asset serialization upgraded.");
        }

        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/Castle Journey.app");
        }

        private static void RequireSavedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before validating or migrating.");
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your open scenes before validating or migrating.");
            }
        }

        private static void RestoreScenes(SceneSetup[] setup)
        {
            if (setup.Any(scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void RepairStaleAttackTriggers()
        {
            foreach (var path in new[] { "Assets/Prefabs/Player.prefab", "Assets/Prefabs/Player 1.prefab" })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (RemoveOrphanedAttackTrigger(prefab))
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePaths[3], OpenSceneMode.Single);
                var changed = false;
                foreach (var root in scene.GetRootGameObjects())
                    changed |= RemoveOrphanedAttackTrigger(root);
                if (changed)
                    EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                RestoreScenes(setup);
            }
        }

        private static bool RemoveOrphanedAttackTrigger(GameObject root)
        {
            var changed = false;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name != "attackTrigger")
                    continue;
                var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (count == 0)
                    continue;
                if (count != 1 || transform.GetComponent<BoxCollider2D>() == null)
                    throw new InvalidOperationException("Unexpected attack trigger structure; refusing to remove components.");
                // GUID 465934e62273057419e3793d809e27a5 has no script in the original repository.
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                changed = true;
            }
            return changed;
        }

        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, "Builds/Android/CastleJourney.apk");
        }

        private static void Build(BuildTarget target, string output)
        {
            Validate();
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new BuildFailedException($"Install the Unity 6000.6.4f1 {target} build support module first.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
            Debug.Log($"Build succeeded: {output}");
        }
    }
}
