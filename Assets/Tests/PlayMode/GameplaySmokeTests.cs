using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace CastleJourney.Tests
{
    public class GameplaySmokeTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private float fixedDeltaTime;
        private InputSettings.BackgroundBehavior backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;

        [SetUp]
        public void SetUp()
        {
            fixedDeltaTime = Time.fixedDeltaTime;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            Time.fixedDeltaTime = fixedDeltaTime;
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
            if (gamepad != null)
                InputSystem.RemoveDevice(gamepad);
            keyboard = null;
            gamepad = null;
            RuntimeType("StaticClass").GetProperty("PreviousScore").SetValue(null, 0f);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator KeyboardMovesJumpsAttacksAndPauses()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            keyboard = InputSystem.AddDevice<Keyboard>();
            var player = Find("PlayerController");
            var body = player.GetComponent<Rigidbody2D>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(.2f);
            Assert.That(body.linearVelocity.x, Is.GreaterThan(5f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.J));
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));
            Assert.That(((Collider2D)Get(player, "attackTrigger")).enabled, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator GamepadMovesAndJumps()
        {
            yield return SceneManager.LoadSceneAsync("World2");
            yield return null;
            gamepad = InputSystem.AddDevice<Gamepad>();
            var body = Find("PlayerController").GetComponent<Rigidbody2D>();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
            yield return new WaitForSeconds(.2f);
            Assert.That(body.linearVelocity.x, Is.GreaterThan(5f));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator MovementSpeedDoesNotDependOnPhysicsTickRate()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            var player = Find("PlayerController");
            ((Behaviour)player).enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            Time.fixedDeltaTime = .02f;
            body.linearVelocity = Vector2.zero;
            for (var i = 0; i < 20; i++)
                Call(player, "Move", 1f, false);
            var speed50Hz = body.linearVelocity.x;
            Time.fixedDeltaTime = .01f;
            body.linearVelocity = Vector2.zero;
            player.GetType().GetField("smoothingVelocity", Fields).SetValue(player, 0f);
            for (var i = 0; i < 40; i++)
                Call(player, "Move", 1f, false);
            Assert.That(speed50Hz, Is.EqualTo(6.5f).Within(.05f));
            Assert.That(body.linearVelocity.x, Is.EqualTo(speed50Hz).Within(.05f));
        }

        [UnityTest]
        public IEnumerator JoystickIgnoresOtherFingersAndResetsWhenDisabled()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var joystick = (Component)Get(Find("PlayerController"), "joystick");
            var rect = (RectTransform)Get(joystick, "background");
            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 42,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.position) + Vector2.right * 150f
            };
            ((IPointerDownHandler)joystick).OnPointerDown(pointer);
            Assert.That((float)joystick.GetType().GetProperty("Horizontal").GetValue(joystick), Is.GreaterThan(.5f));
            ((IPointerUpHandler)joystick).OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 43 });
            Assert.That((float)joystick.GetType().GetProperty("Horizontal").GetValue(joystick), Is.GreaterThan(.5f));
            joystick.gameObject.SetActive(false);
            Assert.That((float)joystick.GetType().GetProperty("Horizontal").GetValue(joystick), Is.Zero);
        }

        [UnityTest]
        public IEnumerator DoubleJumpRejectsAThirdJump()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            var player = Find("PlayerController");
            var body = player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            player.GetType().GetField("m_Grounded", Fields).SetValue(player, true);
            Call(player, "Move", 0f, true);
            Assert.That(body.linearVelocity.y, Is.EqualTo(12f));
            player.GetType().GetField("m_Grounded", Fields).SetValue(player, false);
            Call(player, "Move", 0f, true);
            body.linearVelocity = new Vector2(0, -1);
            Call(player, "Move", 0f, true);
            Assert.That(body.linearVelocity.y, Is.EqualTo(-1f));
        }

        [UnityTest]
        public IEnumerator AttackDamagesTheContactedEnemyOnlyOnce()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            var player = Find("PlayerController");
            Find("PlayerHealth").GetType().GetField("isInvincible").SetValue(Find("PlayerHealth"), true);
            var body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            var originalEnemy = Find("Enemy");
            Assert.That(originalEnemy, Is.Not.Null);
            var clone = UnityEngine.Object.Instantiate(originalEnemy.gameObject);
            var target = clone.GetComponent(RuntimeType("Enemy"));
            var enemies = new[] { originalEnemy, target };
            yield return null;
            foreach (var enemy in enemies)
                ((Behaviour)enemy).enabled = false;
            var trigger = (Collider2D)Get(player, "attackTrigger");
            var targetCollider = target.GetComponent<Collider2D>();
            target.transform.position += trigger.transform.TransformPoint(trigger.offset) - targetCollider.bounds.center;
            var original = (int)Get(target, "enemyLife");
            var other = (int)Get(enemies[0], "enemyLife");
            Call(player, "Attack");
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That((int)Get(target, "enemyLife"), Is.EqualTo(original - 1));
            Assert.That((int)Get(enemies[0], "enemyLife"), Is.EqualTo(other));
            yield return new WaitForFixedUpdate();
            Assert.That((int)Get(target, "enemyLife"), Is.EqualTo(original - 1));
        }

        [UnityTest]
        public IEnumerator LoadingMenuAfterDeathRestoresNormalTime()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            Call(Find("PlayerHealth"), "Damage", 3);
            yield return null;
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Time.timeScale = 0f;
            yield return SceneManager.LoadSceneAsync("EndGame");
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator MovingPlatformAllowsWalkingAndDoubleJumping()
        {
            yield return SceneManager.LoadSceneAsync("World1");
            yield return null;
            var player = Find("PlayerController");
            var body = player.GetComponent<Rigidbody2D>();
            Find("PlayerHealth").GetType().GetField("isInvincible").SetValue(Find("PlayerHealth"), true);
            var platform = Find("MovingPlatform");
            var surface = platform.GetComponent<Collider2D>();
            var playerCollider = body.GetComponents<Collider2D>().First(collider => !collider.isTrigger);
            var footOffset = body.position.y - playerCollider.bounds.min.y;
            body.position = new Vector2(surface.bounds.center.x, surface.bounds.max.y + footOffset + .03f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.3f);
            Assert.That((bool)Get(player, "m_Grounded"), Is.True, "The real platform must register as ground.");
            Assert.That(player.transform.parent, Is.Null, "Physics bodies must not be parented to the moving platform.");
            var relative = body.position - (Vector2)surface.bounds.center;
            yield return new WaitForSeconds(.15f);
            Assert.That(Vector2.Distance(body.position - (Vector2)surface.bounds.center, relative), Is.LessThan(.15f),
                "An idle player must ride the platform.");

            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            relative = body.position - (Vector2)surface.bounds.center;
            yield return new WaitForSeconds(.12f);
            Assert.That((body.position - (Vector2)surface.bounds.center).x, Is.GreaterThan(relative.x + .2f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            relative = body.position - (Vector2)surface.bounds.center;
            yield return new WaitForSeconds(.18f);
            Assert.That((body.position - (Vector2)surface.bounds.center).x, Is.LessThan(relative.x - .2f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(.1f);
            Assert.That((bool)Get(player, "m_Grounded"), Is.True);
            Call(player, "Jump");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That((int)Get(player, "jumpsUsed"), Is.EqualTo(1));
            yield return new WaitForSeconds(.18f);
            Call(player, "Jump");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That((int)Get(player, "jumpsUsed"), Is.EqualTo(2));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(8f));
        }

        [UnityTest]
        public IEnumerator RealWorldLandingRestoresBothJumps()
        {
            foreach (var name in new[] { "World1", "World2", "World3" })
            {
                yield return SceneManager.LoadSceneAsync(name);
                yield return null;
                var player = Find("PlayerController");
                Find("PlayerHealth").GetType().GetField("isInvincible").SetValue(Find("PlayerHealth"), true);
                var body = player.GetComponent<Rigidbody2D>();
                var deadline = Time.realtimeSinceStartup + 3f;
                while (!(bool)Get(player, "m_Grounded") && Time.realtimeSinceStartup < deadline)
                    yield return new WaitForFixedUpdate();
                Assert.That((bool)Get(player, "m_Grounded"), Is.True, name + " initial landing");
                Call(player, "Jump");
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.That((int)Get(player, "jumpsUsed"), Is.EqualTo(1), name + " first jump");
                yield return new WaitForSeconds(.18f);
                Call(player, "Jump");
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.That((int)Get(player, "jumpsUsed"), Is.EqualTo(2), name + " double jump");
                Assert.That(body.linearVelocity.y, Is.GreaterThan(8f), name + " second impulse");
                deadline = Time.realtimeSinceStartup + 5f;
                while (!(bool)Get(player, "m_Grounded") && Time.realtimeSinceStartup < deadline)
                    yield return new WaitForFixedUpdate();
                Assert.That((bool)Get(player, "m_Grounded"), Is.True, name + " landing after double jump");
                Assert.That((int)Get(player, "jumpsUsed"), Is.Zero, name + " jump count reset");
            }
        }

        [UnityTest]
        public IEnumerator MainMenuLoadsAndStartButtonLoadsWorld1()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var frame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(frame));
            Assert.That(EventSystem.current, Is.Not.Null);
            var start = UnityEngine.Object.FindObjectsByType<Button>()
                .First(button => button.name == "StartButton");
            start.onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "World1" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("World1"));
            yield return null;
            Assert.That(Find("PlayerController"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator World1GameplayWorks()
        {
            yield return CheckWorld("World1");
        }

        [UnityTest]
        public IEnumerator World2GameplayWorks()
        {
            yield return CheckWorld("World2");
        }

        [UnityTest]
        public IEnumerator World3GameplayWorks()
        {
            yield return CheckWorld("World3");
        }

        [UnityTest]
        public IEnumerator EndGameDisplaysCarriedScore()
        {
            RuntimeType("StaticClass").GetProperty("PreviousScore").SetValue(null, 1234f);
            yield return SceneManager.LoadSceneAsync("EndGame");
            yield return null;
            var score = Find("EndScore");
            Assert.That(((Text)Get(score, "scoreText")).text, Is.EqualTo("1234"));
            Assert.That(EventSystem.current, Is.Not.Null);
        }

        private static IEnumerator CheckWorld(string name)
        {
            yield return SceneManager.LoadSceneAsync(name);
            yield return null;
            yield return new WaitForFixedUpdate();
            var player = Find("PlayerController");
            var health = Find("PlayerHealth");
            var score = Find("Score");
            Assert.That(player, Is.Not.Null);
            Assert.That(health, Is.Not.Null);
            Assert.That(score, Is.Not.Null);
            Assert.That((int)Get(health, "Health"), Is.EqualTo(3));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(EventSystem.current.currentInputModule, Is.TypeOf<InputSystemUIInputModule>());
            Assert.That(UnityEngine.Object.FindAnyObjectByType<CinemachineBrain>().ActiveVirtualCamera, Is.Not.Null);

            var joystick = (Component)Get(player, "joystick");
            var handle = (RectTransform)Get(joystick, "handle");
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, handle.position) + new Vector2(100, 0)
            };
            ((IPointerDownHandler)joystick).OnPointerDown(pointer);
            var body = player.GetComponent<Rigidbody2D>();
            var startX = body.position.x;
            yield return new WaitForSeconds(0.25f);
            Assert.That(body.position.x, Is.GreaterThan(startX));
            ((IPointerUpHandler)joystick).OnPointerUp(pointer);

            Call(player, "Jump");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0));

            Call(player, "Attack");
            yield return null;
            yield return null;
            Assert.That(((Collider2D)Get(player, "attackTrigger")).enabled, Is.True);
            yield return new WaitForSeconds(0.3f);
            Assert.That(((Collider2D)Get(player, "attackTrigger")).enabled, Is.False);

            var oldScore = (float)Get(score, "score");
            Call(score, "AddScore", 100);
            Assert.That((float)Get(score, "score"), Is.EqualTo(oldScore + 100));
            Call(health, "Damage", 1);
            Assert.That((int)Get(health, "Health"), Is.EqualTo(2));
            Call(health, "AddLife", 1);
            Assert.That((int)Get(health, "Health"), Is.EqualTo(3));

            var pause = Find("Pause_script");
            Call(pause, "Pausegame");
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(((GameObject)Get(pause, "pauseMenuUI")).activeSelf, Is.True);
            Call(pause, "Resumegame");
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(((GameObject)Get(pause, "gameCanvasUI")).activeSelf, Is.True);

            Call(health, "Damage", 3);
            yield return null;
            yield return null;
            Assert.That(((GameObject)Get(health, "endgameCanvas")).activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
        }

        private static Type RuntimeType(string name)
        {
            return Type.GetType(name + ", Assembly-CSharp", true);
        }

        private static Component Find(string name)
        {
            return UnityEngine.Object.FindAnyObjectByType(RuntimeType(name)) as Component;
        }

        private static object Get(Component target, string name)
        {
            Assert.That(target, Is.Not.Null, name);
            return target.GetType().GetField(name, Fields).GetValue(target);
        }

        private static void Call(Component target, string name, params object[] arguments)
        {
            Assert.That(target, Is.Not.Null, name);
            target.GetType().GetMethod(name).Invoke(target, arguments);
        }
    }
}
