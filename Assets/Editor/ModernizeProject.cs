using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace CastleJourney.Editor
{
    public static class ModernizeProject
    {
        public const string SettingsFolder = "Assets/Settings";
        public const string InputPath = SettingsFolder + "/CastleJourney.inputactions";
        public const string PipelinePath = SettingsFolder + "/CastleJourneyURP.asset";
        public const string RendererPath = SettingsFolder + "/CastleJourney2DRenderer.asset";
        private static readonly Vector2 ReferenceResolution = new Vector2(1280, 720);
        private static Sprite pauseBackground;

        [MenuItem("Castle Journey/Modernize Project")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before migrating.");
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before migrating.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets", "Settings");
            try
            {
                CreateInput();
                ConfigureRendering();
                ConfigureSettings();
                foreach (var path in Unity6Migration.ScenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    UpgradeScene(scene.GetRootGameObjects());
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"Modernized {path}");
                }
                foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath))
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        UpgradeScene(new[] { root });
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (setup.Any(scene => scene.isLoaded && scene.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Unity6Migration.Validate();
        }

        private static InputActionAsset CreateInput()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (existing != null)
                return existing;
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "Castle Journey";
            var gameplay = actions.AddActionMap("Gameplay");
            var move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
            var jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");
            var attack = gameplay.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Keyboard>/j");
            attack.AddBinding("<Keyboard>/leftCtrl");
            attack.AddBinding("<Gamepad>/buttonWest");
            var pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
            // Gameplay and UI use distinct submit/attack buttons to avoid firing both on resume.
            var defaults = new DefaultInputActions();
            var ui = InputActionMap.FromJson(defaults.asset.ToJson()).First(map => map.name == "UI");
            actions.AddActionMap(ui);
            defaults.Dispose();
            File.WriteAllText(InputPath, actions.ToJson());
            UnityEngine.Object.DestroyImmediate(actions);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }

        private static Material ConfigureRendering()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("m_DefaultMaterialType").intValue = 1; // Unlit sprites.
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 1;
                pipeline.renderScale = 1f;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            var oldQuality = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(oldQuality, false);

            const string materialPath = SettingsFolder + "/SpriteUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                    throw new InvalidOperationException("URP Sprite-Unlit-Default shader is unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            return material;
        }

        private static void ConfigureSettings()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
        }

        private static void UpgradeScene(GameObject[] roots)
        {
            // Opening a scene unloads unused native assets, even when C# locals still reference them.
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SettingsFolder + "/SpriteUnlit.mat");
            var components = roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var start = components.OfType<Button>().FirstOrDefault(button => button.name == "StartButton");
            if (start != null && start.image != null)
                pauseBackground = start.image.sprite;
            var manager = Type.GetType("Unity.Cinemachine.Editor.CinemachineUpgradeManager, Unity.Cinemachine.Editor", true);
            var upgrade = manager.GetMethod("UpgradeSingleObject", BindingFlags.Static | BindingFlags.Public);
            foreach (var camera in components.OfType<MonoBehaviour>()
                .Where(component => component != null && component.GetType().Name == "CinemachineVirtualCamera").ToArray())
            {
                if (PrefabUtility.IsPartOfPrefabInstance(camera))
                    throw new InvalidOperationException("A camera prefab needs the full Cinemachine project upgrader.");
                var gameObject = camera.gameObject;
                upgrade.Invoke(null, new object[] { gameObject });
                var modern = gameObject.GetComponent<CinemachineCamera>();
                if (modern == null || modern.Target.TrackingTarget == null)
                    throw new InvalidOperationException("Cinemachine conversion lost the camera or tracking target.");
            }
            components = roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            foreach (var composer in components.OfType<CinemachinePositionComposer>())
                composer.Damping = new Vector3(.35f, .5f, 0f);
            foreach (var camera in components.OfType<Camera>())
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
            }
            foreach (var renderer in components.OfType<Renderer>())
            {
                if (!(renderer is SpriteRenderer) && !(renderer is TilemapRenderer))
                    continue;
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    if (materials[i] == null || materials[i].shader.name == "Sprites/Default")
                        materials[i] = spriteMaterial;
                renderer.sharedMaterials = materials;
            }
            foreach (var system in components.OfType<EventSystem>())
            {
                foreach (var old in system.GetComponents<StandaloneInputModule>())
                    UnityEngine.Object.DestroyImmediate(old);
                var module = system.GetComponent<InputSystemUIInputModule>()
                    ?? system.gameObject.AddComponent<InputSystemUIInputModule>();
                module.actionsAsset = actions;
                module.point = Reference(actions, "UI/Point");
                module.leftClick = Reference(actions, "UI/Click");
                module.rightClick = Reference(actions, "UI/RightClick");
                module.middleClick = Reference(actions, "UI/MiddleClick");
                module.scrollWheel = Reference(actions, "UI/ScrollWheel");
                module.move = Reference(actions, "UI/Navigate");
                module.submit = Reference(actions, "UI/Submit");
                module.cancel = Reference(actions, "UI/Cancel");
                if (system.firstSelectedGameObject == null)
                {
                    var first = components.OfType<Button>().FirstOrDefault(button => button.name == "StartButton")
                        ?? components.OfType<Button>().FirstOrDefault(button => button.name == "MainMenuButton");
                    if (first != null)
                        system.firstSelectedGameObject = first.gameObject;
                }
            }
            foreach (var player in components.OfType<PlayerController>())
            {
                var input = player.GetComponent<GameplayInput>() ?? player.gameObject.AddComponent<GameplayInput>();
                var serializedInput = new SerializedObject(input);
                serializedInput.FindProperty("actions").objectReferenceValue = actions;
                serializedInput.ApplyModifiedPropertiesWithoutUndo();
                var settings = new SerializedObject(player);
                settings.FindProperty("runSpeed").floatValue = 6.5f;
                settings.FindProperty("m_AirControl").boolValue = true;
                settings.FindProperty("jumpSpeed").floatValue = 12f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                player.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            foreach (var platform in components.OfType<MovingPlatform>())
            {
                var body = platform.GetComponent<Rigidbody2D>() ?? platform.gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            foreach (var canvas in components.OfType<Canvas>().Where(canvas => canvas.isRootCanvas))
                NormalizeCanvas(canvas);
            NormalizeHud(roots);
            NormalizeEndGame(roots);
        }

        private static InputActionReference Reference(InputActionAsset actions, string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .First(reference => reference.action.id == actions.FindAction(name, true).id);
        }

        private static void NormalizeCanvas(Canvas canvas)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                return;
            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var safe = canvas.transform.Find("SafeArea") as RectTransform;
            if (safe == null)
            {
                safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeArea)).GetComponent<RectTransform>();
                safe.SetParent(canvas.transform, false);
                Stretch(safe);
                foreach (var child in canvas.transform.Cast<Transform>().Where(child => child != safe).ToArray())
                    child.SetParent(safe, false);
            }
        }

        private static void NormalizeHud(GameObject[] roots)
        {
            var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach (var controls in all.Where(transform => transform.name == "Controls").OfType<RectTransform>())
                Stretch(controls);
            foreach (var joystick in roots.SelectMany(root => root.GetComponentsInChildren<FixedJoystick>(true)))
            {
                Place((RectTransform)joystick.transform, Vector2.zero, new Vector2(100, 100), new Vector2(140, 140));
                if (joystick.handle != null)
                    joystick.handle.sizeDelta = new Vector2(64, 64);
            }
            foreach (var button in roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)))
            {
                if (button.name == "Button Attack")
                {
                    Place((RectTransform)button.transform, Vector2.right, new Vector2(-92, 94), new Vector2(104, 104));
                    NormalizeTouchLabel(button);
                }
                else if (button.name == "Button Jump")
                {
                    Place((RectTransform)button.transform, Vector2.right, new Vector2(-222, 94), new Vector2(104, 104));
                    NormalizeTouchLabel(button);
                }
                else if (button.name == "Pause Button")
                {
                    Place((RectTransform)button.transform, Vector2.one, new Vector2(-52, -52), new Vector2(72, 72));
                    NormalizePauseIcon(button);
                }
            }
            foreach (var health in roots.SelectMany(root => root.GetComponentsInChildren<PlayerHealth>(true)))
            {
                if (health.Hearts == null || health.Hearts.Length == 0 || health.Hearts[0] == null)
                    continue;
                var life = health.Hearts[0].transform.parent as RectTransform;
                if (life == null)
                    continue;
                Place(life, Vector2.up, new Vector2(104, -48), new Vector2(144, 48));
                for (var i = 0; i < health.Hearts.Length; i++)
                    if (health.Hearts[i] != null)
                        Place(health.Hearts[i].rectTransform, new Vector2(.5f, .5f),
                            new Vector2((i - 1) * 48, 0), new Vector2(40, 40));
            }
            foreach (var score in roots.SelectMany(root => root.GetComponentsInChildren<Score>(true)))
            {
                if (score.scoreText == null)
                    continue;
                Place(score.scoreText.rectTransform, Vector2.up, new Vector2(270, -52), new Vector2(160, 64));
                score.scoreText.fontSize = 24;
                score.scoreText.resizeTextForBestFit = false;
                score.scoreText.alignment = TextAnchor.MiddleLeft;
            }
            foreach (var boss in roots.SelectMany(root => root.GetComponentsInChildren<Boss>(true)))
            {
                var slider = new SerializedObject(boss).FindProperty("healthBar").objectReferenceValue as Slider;
                if (slider == null)
                    continue;
                Place((RectTransform)slider.transform, new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(320, 20));
                slider.transition = Selectable.Transition.None;
                slider.interactable = false;
                slider.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private static void NormalizePauseIcon(Button button)
        {
            if (pauseBackground == null || button.image == null)
                return;
            button.image.sprite = pauseBackground;
            button.image.type = Image.Type.Sliced;
            button.image.color = Color.white;
            foreach (var side in new[] { -1, 1 })
            {
                var name = side < 0 ? "PauseLeft" : "PauseRight";
                var rect = button.transform.Find(name) as RectTransform;
                if (rect == null)
                {
                    rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    rect.SetParent(button.transform, false);
                }
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(side * 9, 0);
                rect.sizeDelta = new Vector2(7, 28);
                var image = rect.GetComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }

        private static void NormalizeTouchLabel(Button button)
        {
            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 16;
                text.resizeTextMaxSize = 22;
                text.color = new Color32(48, 44, 60, 255);
                Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(8, 8);
                text.rectTransform.offsetMax = new Vector2(-8, -8);
            }
        }

        private static void NormalizeEndGame(GameObject[] roots)
        {
            var transforms = roots.SelectMany(root => root.GetComponentsInChildren<RectTransform>(true)).ToArray();
            if (!transforms.Any(rect => rect.name == "YOUWIN_Text"))
                return;
            foreach (var rect in transforms)
            {
                if (rect.name == "YOUWIN_Text")
                    Place(rect, new Vector2(.5f, .5f), new Vector2(0, 100), new Vector2(360, 48));
                else if (rect.name == "YOURSCORE_Text")
                    Place(rect, new Vector2(.5f, .5f), new Vector2(0, 32), new Vector2(360, 36));
                else if (rect.name == "SCORE_TEXT")
                    Place(rect, new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(360, 48));
                else if (rect.name == "MAINMENU_BUTTON" || rect.name == "MainMenuButton")
                    Place(rect, new Vector2(.5f, .5f), new Vector2(0, -100), new Vector2(240, 64));
                else if (rect.name == "EXIT_BUTTON" || rect.name == "ExitButton")
                    Place(rect, new Vector2(.5f, .5f), new Vector2(0, -182), new Vector2(240, 64));
                else
                    continue;
                foreach (var text in rect.GetComponentsInChildren<Text>(true))
                {
                    text.alignment = TextAnchor.MiddleCenter;
                    text.resizeTextForBestFit = false;
                    text.fontSize = rect.name == "YOUWIN_Text" || rect.name == "SCORE_TEXT" ? 32 : 24;
                }
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            var safe = canvas != null ? canvas.transform.Find("SafeArea") : null;
            if (safe != null && rect.parent != safe)
            {
                // Hearts stay within their shared Life container; other HUD items use screen anchors.
                if (rect.parent == null || rect.parent.name != "Life")
                    rect.SetParent(safe, false);
            }
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
