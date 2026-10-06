using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CastleJourney.Tests
{
    public class RenderingSmokeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Time.timeScale = 1f;
        }

        [UnityTest] public IEnumerator MainMenuRenders() => CheckScene("MainMenu");
        [UnityTest] public IEnumerator World1Renders() => CheckScene("World1");
        [UnityTest] public IEnumerator World2Renders() => CheckScene("World2");
        [UnityTest] public IEnumerator World3Renders() => CheckScene("World3");
        [UnityTest] public IEnumerator EndGameRenders() => CheckScene("EndGame");

        private static IEnumerator CheckScene(string name)
        {
            yield return SceneManager.LoadSceneAsync(name);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2400, 1080), new Vector2Int(1024, 768) })
            {
                Capture(name, camera, size);
                yield return null;
            }
        }

        private static void Capture(string scene, Camera camera, Vector2Int size)
        {
            var canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas).ToArray();
            var modes = canvases.Select(canvas => canvas.renderMode).ToArray();
            var cameras = canvases.Select(canvas => canvas.worldCamera).ToArray();
            var distances = canvases.Select(canvas => canvas.planeDistance).ToArray();
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = camera.aspect;
            var target = new RenderTexture(size.x, size.y, 24);
            var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.aspect = (float)size.x / size.y;
                for (var i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 1f + i * .01f;
                }
                Canvas.ForceUpdateCanvases();
                if (scene == "EndGame")
                {
                    var items = Object.FindObjectsByType<Button>().Where(button => button.isActiveAndEnabled)
                        .Select(button => (RectTransform)button.transform)
                        .Concat(Object.FindObjectsByType<Text>()
                            .Where(text => text.name == "YOUWIN_Text" || text.name == "YOURSCORE_Text" || text.name == "SCORE_TEXT")
                            .Select(text => text.rectTransform)).ToArray();
                    for (var i = 0; i < items.Length; i++)
                        for (var j = i + 1; j < items.Length; j++)
                            Assert.That(ScreenBounds(items[i], camera).Overlaps(ScreenBounds(items[j], camera)), Is.False,
                                $"{items[i].name} overlaps {items[j].name} at {size}");
                }
                foreach (var button in Object.FindObjectsByType<Button>())
                {
                    if (button.name != "Button Jump" && button.name != "Button Attack" && button.name != "Pause Button")
                        continue;
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var pixel = camera.WorldToScreenPoint(corner);
                        Assert.That(pixel.x, Is.InRange(-1f, size.x + 1f), $"{scene}/{button.name} horizontal clipping at {size}");
                        Assert.That(pixel.y, Is.InRange(-1f, size.y + 1f), $"{scene}/{button.name} vertical clipping at {size}");
                    }
                }
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                var brightness = pixels.Average(pixel => (pixel.r + pixel.g + pixel.b) / 3f);
                var magenta = pixels.Count(pixel => pixel.r > 220 && pixel.b > 220 && pixel.g < 40);
                Assert.That(brightness, Is.GreaterThan(5f), $"{scene}: blank or black render");
                Assert.That((float)magenta / pixels.Length, Is.LessThan(.01f), $"{scene}: missing-shader pixels");
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Validation/Screenshots"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, $"{scene}-{size.x}x{size.y}.png"), image.EncodeToPNG());
            }
            finally
            {
                for (var i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = modes[i];
                    canvases[i].worldCamera = cameras[i];
                    canvases[i].planeDistance = distances[i];
                }
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static Rect ScreenBounds(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var minimum = camera.WorldToScreenPoint(corners[0]);
            var maximum = camera.WorldToScreenPoint(corners[2]);
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
    }
}
