using System;
using System.Linq;
using Unity.Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.That(settings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1));
            Assert.That(Physics2D.gravity, Is.EqualTo(new Vector2(0, -9.81f)));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.02f).Within(0.00001f));
        }

        [Test]
        public void SupportedCinemachineIsInstalled()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
            Assert.That(package.version, Is.EqualTo("6.6.0"));
        }

        [Test]
        public void InputActionsHaveKeyboardGamepadAndUiBindings()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/CastleJourney.inputactions");
            Assert.That(actions, Is.Not.Null);
            foreach (var name in new[] { "Move", "Jump", "Attack", "Pause" })
            {
                var action = actions.FindAction("Gameplay/" + name, true);
                Assert.That(action.bindings.Any(binding => binding.path.StartsWith("<Keyboard>")), Is.True, name);
                Assert.That(action.bindings.Any(binding => binding.path.StartsWith("<Gamepad>")), Is.True, name);
            }
            foreach (var name in new[] { "Point", "Click", "Navigate", "Submit", "Cancel" })
                Assert.That(actions.FindAction("UI/" + name), Is.Not.Null, name);
            Assert.That(actions.FindAction("Gameplay/Attack").bindings.Any(binding => binding.path == "<Mouse>/leftButton"),
                Is.False, "UI clicks must not also fire gameplay attacks.");
        }

        [TestCase("Assets/Scenes/UI/MainMenu.unity")]
        [TestCase("Assets/Scenes/World1.unity")]
        [TestCase("Assets/Scenes/World2.unity")]
        [TestCase("Assets/Scenes/World3.unity")]
        [TestCase("Assets/Scenes/UI/EndGame.unity")]
        public void ScenesUseModernInputAndConsistentCanvasScaling(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            Assert.That(components.OfType<EventSystem>().Count(), Is.EqualTo(1));
            Assert.That(components.OfType<StandaloneInputModule>(), Is.Empty);
            var module = components.OfType<InputSystemUIInputModule>().Single();
            Assert.That(module.actionsAsset, Is.Not.Null);
            foreach (var reference in new[] { module.point, module.leftClick, module.move, module.submit, module.cancel })
            {
                Assert.That(reference, Is.Not.Null);
                Assert.That(reference.action, Is.Not.Null);
            }
            foreach (var canvas in components.OfType<Canvas>().Where(canvas => canvas.isRootCanvas))
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null, canvas.name);
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize), canvas.name);
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280, 720)), canvas.name);
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(.5f), canvas.name);
                Assert.That(canvas.transform.Find("SafeArea"), Is.Not.Null, canvas.name);
            }
            Assert.That(components.OfType<MonoBehaviour>().Any(component =>
                component != null && component.GetType().Name == "CinemachineVirtualCamera"), Is.False);
            foreach (var renderer in components.OfType<Renderer>().Where(renderer =>
                renderer is SpriteRenderer || renderer is TilemapRenderer))
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null, renderer.name);
                    Assert.That(material.shader.name, Does.StartWith("Universal Render Pipeline/"), renderer.name);
                }
        }

        [Test]
        public void QualityLevelsUseSameUrpAsset()
        {
            var original = QualitySettings.GetQualityLevel();
            try
            {
                for (var i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    Assert.That(QualitySettings.renderPipeline, Is.EqualTo(GraphicsSettings.defaultRenderPipeline));
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(original, false);
            }
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
            var camera = components.OfType<CinemachineCamera>().FirstOrDefault();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.Target.TrackingTarget, Is.Not.Null);
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
