using System;
using System.Linq;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace CastleJourney.Tests
{
    public class MigrationTests
    {
        private SceneSetup[] setup;

        [SetUp]
        public void SetUp()
        {
            setup = EditorSceneManager.GetSceneManagerSetup();
        }

        [TearDown]
        public void TearDown()
        {
            if (setup.Any(scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BuildSceneIndicesArePreserved()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Scenes/UI/MainMenu.unity",
                "Assets/Scenes/World1.unity",
                "Assets/Scenes/World2.unity",
                "Assets/Scenes/World3.unity",
                "Assets/Scenes/UI/EndGame.unity"
            }, EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
        }

        [Test]
        public void RendererAndInputRemainCompatible()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.6.4f1"));
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Null);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.That(settings.FindProperty("activeInputHandler").intValue, Is.Zero);
            Assert.That(Physics2D.gravity, Is.EqualTo(new Vector2(0, -9.81f)));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.02f).Within(0.00001f));
        }

        [Test]
        public void CinemachineIsNotSilentlyReplacedByBundledVersion()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
            Assert.That(package.version, Is.EqualTo("2.10.7"));
        }

        [TestCase("Assets/Scenes/UI/MainMenu.unity")]
        [TestCase("Assets/Scenes/World1.unity")]
        [TestCase("Assets/Scenes/World2.unity")]
        [TestCase("Assets/Scenes/World3.unity")]
        [TestCase("Assets/Scenes/UI/EndGame.unity")]
        public void BuildScenesHaveNoMissingScriptsOrBrokenReferences(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                        Is.Zero, $"{path}/{transform.name}");
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        if (component == null)
                            continue;
                        var properties = new SerializedObject(component).GetIterator();
                        while (properties.Next(true))
                        {
                            if (properties.propertyType == SerializedPropertyType.ObjectReference
                                && properties.objectReferenceValue == null)
                                Assert.That(properties.objectReferenceEntityIdValue, Is.EqualTo(EntityId.None),
                                    $"{path}/{transform.name}/{component.GetType().Name}/{properties.propertyPath}");
                        }
                    }
                }
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                    AssertCallbacks(button.onClick, $"{path}/{button.name}");
            }
        }

        [TestCase("Assets/Scenes/World1.unity")]
        [TestCase("Assets/Scenes/World2.unity")]
        [TestCase("Assets/Scenes/World3.unity")]
        public void WorldsRetainCameraAndTileSprites(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var camera = components.OfType<CinemachineVirtualCamera>().FirstOrDefault();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.Follow, Is.Not.Null);
            Assert.That(components.OfType<CinemachineBrain>(), Is.Not.Empty);
            var tileCount = 0;
            foreach (var tilemap in components.OfType<Tilemap>())
            {
                foreach (var position in tilemap.cellBounds.allPositionsWithin)
                {
                    var tile = tilemap.GetTile(position);
                    if (tile == null)
                        continue;
                    tileCount++;
                    Assert.That(tilemap.GetSprite(position), Is.Not.Null,
                        $"{path}/{tilemap.name}/{position}");
                }
            }
            Assert.That(tileCount, Is.GreaterThan(0));
            Assert.That(components.OfType<SpriteRenderer>().Count(renderer => renderer.sprite != null),
                Is.GreaterThan(0));
        }

        [Test]
        public void GamePrefabsHaveNoMissingScripts()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            var prefabs = AssetDatabase.GetDependencies(scenes, true)
                .Where(path => path.EndsWith(".prefab", StringComparison.Ordinal))
                .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath)).Distinct();
            foreach (var path in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                        Is.Zero, $"{path}/{transform.name}");
            }
        }

        private static void AssertCallbacks(UnityEvent callbacks, string context)
        {
            for (var i = 0; i < callbacks.GetPersistentEventCount(); i++)
            {
                var method = callbacks.GetPersistentMethodName(i);
                if (string.IsNullOrEmpty(method))
                    continue;
                var target = callbacks.GetPersistentTarget(i);
                Assert.That(target, Is.Not.Null, $"{context}/{method}");
                Assert.That(target.GetType().GetMethods().Any(candidate => candidate.Name == method),
                    Is.True, $"{context}/{method}");
            }
        }
    }
}
