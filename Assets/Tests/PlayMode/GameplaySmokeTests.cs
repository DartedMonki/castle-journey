using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CastleJourney.Tests
{
    public class GameplaySmokeTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            RuntimeType("StaticClass").GetProperty("PreviousScore").SetValue(null, 0f);
            yield return SceneManager.LoadSceneAsync("MainMenu");
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
            var start = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
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
            Assert.That(UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>().ActiveVirtualCamera, Is.Not.Null);

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
            return UnityEngine.Object.FindFirstObjectByType(RuntimeType(name)) as Component;
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
