using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NumMatch.Managers;

namespace NumMatch.Testing.PlayMode
{
    /// <summary>
    /// PlayMode UI Tests — kiểm tra UIController hiển thị đúng.
    /// YÊU CẦU: Scene "Main" phải có trong Build Settings (File > Build Settings > Add Open Scenes).
    /// </summary>
    public class UITests
    {
        // ─── Setup: Load scene Main trước mỗi test ───────────────────────────

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Load scene Main nếu chưa load hoặc không phải active scene
            if (SceneManager.GetActiveScene().name != "Main")
            {
                SceneManager.LoadScene("Main");
                // Đợi scene load xong
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return null;
            }
        }

        // ─── Helper ──────────────────────────────────────────────────────────

        private UIController GetUIController()
        {
            var ui = Object.FindObjectOfType<UIController>(true);
            Assert.IsNotNull(ui,
                "⚠️ Không tìm thấy UIController trong Scene. " +
                "Kiểm tra Scene 'Main' đã được thêm vào Build Settings chưa.");
            return ui;
        }

        private string GetTMPText(object component)
        {
            if (component == null) return null;
            var prop = component.GetType().GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(component) as string;
        }

        // ─── Test Cases ───────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator UI_01_CanvasScaler_ConfiguredCorrectly()
        {
            var canvasScaler = Object.FindObjectOfType<CanvasScaler>(true);
            Assert.IsNotNull(canvasScaler, "Phải có CanvasScaler trong Scene Main.");

            Assert.AreEqual(
                CanvasScaler.ScaleMode.ScaleWithScreenSize,
                canvasScaler.uiScaleMode,
                "Scale Mode phải là ScaleWithScreenSize");

            Assert.AreEqual(
                new Vector2(1080, 1920),
                canvasScaler.referenceResolution,
                "Reference Resolution phải là 1080x1920");

            yield return null;
        }

        [UnityTest]
        public IEnumerator UI_02_UIController_References_NotNull()
        {
            var uiController = GetUIController();

            CheckPrivateField(uiController, "_stageText");
            CheckPrivateField(uiController, "_addsRemainingText");  // field thực tế trong UIController
            CheckPrivateField(uiController, "_addButton");
            CheckPrivateField(uiController, "_winPopup");
            CheckPrivateField(uiController, "_losePopup");
            CheckPrivateField(uiController, "_settingPopup");

            yield return null;
        }

        [UnityTest]
        public IEnumerator UI_03_WinPopup_ShowsCorrectly()
        {
            var uiController = GetUIController();

            var winPopupField = typeof(UIController).GetField(
                "_winPopup", BindingFlags.NonPublic | BindingFlags.Instance);
            var winPopupGO = winPopupField?.GetValue(uiController) as GameObject;

            // Đảm bảo popup đang ẩn trước khi test
            if (winPopupGO != null) winPopupGO.SetActive(false);

            uiController.ShowWinPopup(1, () => { });
            yield return new WaitForSeconds(0.6f); // chờ DOTween animation

            Assert.IsNotNull(winPopupGO, "WinPopup GameObject không được null (chưa gán trong Inspector?)");
            Assert.IsTrue(winPopupGO.activeSelf, "WinPopup phải active sau khi gọi ShowWinPopup");
        }

        [UnityTest]
        public IEnumerator UI_04_LosePopup_ShowsLevelText()
        {
            var uiController = GetUIController();

            var losePopupField = typeof(UIController).GetField(
                "_losePopup", BindingFlags.NonPublic | BindingFlags.Instance);
            var losePopupGO = losePopupField?.GetValue(uiController) as GameObject;
            if (losePopupGO != null) losePopupGO.SetActive(false);

            uiController.ShowLosePopup(99, () => { });
            yield return new WaitForSeconds(0.6f);

            Assert.IsNotNull(losePopupGO, "LosePopup GameObject không được null (chưa gán trong Inspector?)");
            Assert.IsTrue(losePopupGO.activeSelf, "LosePopup phải active sau khi gọi ShowLosePopup");

            // Kiểm tra info text hiển thị đúng level (dùng reflection tránh phụ thuộc TMPro)
            var infoField = typeof(UIController).GetField(
                "_loseInfoText", BindingFlags.NonPublic | BindingFlags.Instance);
            string actual = GetTMPText(infoField?.GetValue(uiController));
            if (actual != null)
            {
                Assert.AreEqual("Failed at Level 99", actual,
                    "LoseInfoText phải hiện đúng 'Failed at Level 99'");
            }
        }

        [UnityTest]
        public IEnumerator UI_05_AddBadge_UpdatesText()
        {
            var uiController = GetUIController();

            uiController.SetAddsRemaining(7);
            yield return null; // đợi 1 frame

            var field = typeof(UIController).GetField(
                "_addsRemainingText", BindingFlags.NonPublic | BindingFlags.Instance);
            string actual = GetTMPText(field?.GetValue(uiController));
            if (actual != null)
            {
                Assert.AreEqual("7", actual,
                    "Badge số lượt Add phải hiển thị đúng con số (7)");
            }
        }

        // ─── Private helpers ──────────────────────────────────────────────────

        private static void CheckPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' không tồn tại trong UIController!");

            var value = field.GetValue(obj);
            Assert.IsNotNull(value,
                $"⚠️ Missing Reference: '{fieldName}' chưa được kéo thả vào Inspector của UIController!");
        }
    }
}
