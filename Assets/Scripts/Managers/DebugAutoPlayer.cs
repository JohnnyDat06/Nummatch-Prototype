using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using NumMatch.Core;
using NumMatch.Managers;

/// <summary>
/// Debug AI tự động chơi NumMatch.
/// Attach vào bất kỳ GameObject nào trong scene.
/// </summary>
public class DebugAutoPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager _boardManager;

    [Header("Mode")]
    [Tooltip("ForceWin = match hết pairs kể cả gem để thắng.\nForceLose = bỏ qua gem, dùng hết Add, gây deadlock để thua.")]
    [SerializeField] private DebugMode _mode = DebugMode.ForceWin;

    [Header("Speed")]
    [Tooltip("Delay (giây) giữa mỗi lần match. 0.1 để xem rõ, 0 để tức thì.")]
    [SerializeField] private float _stepDelay = 0.3f;

    public enum DebugMode
    {
        ForceWin,
        ForceLose,
    }

    private bool _running = false;

    // ─── Inspector Buttons via ContextMenu ─────────────────────────────────
    [ContextMenu("▶ Run Auto Play")]
    public void RunAutoPlay()
    {
        if (_boardManager == null)
        {
            _boardManager = FindObjectOfType<BoardManager>();
        }
        if (_boardManager == null)
        {
            Debug.LogError("[DebugAutoPlayer] Không tìm thấy BoardManager!");
            return;
        }
        if (_running)
        {
            Debug.LogWarning("[DebugAutoPlayer] Đang chạy rồi, bỏ qua.");
            return;
        }

        if (_mode == DebugMode.ForceWin)
            StartCoroutine(RunForceWin());
        else
            StartCoroutine(RunForceLose());
    }

    [ContextMenu("⏹ Stop")]
    public void Stop()
    {
        StopAllCoroutines();
        _running = false;
        Debug.Log("[DebugAutoPlayer] Dừng.");
    }

    [ContextMenu("💡 Show Hints")]
    public void ShowHints()
    {
        if (_boardManager == null) _boardManager = FindObjectOfType<BoardManager>();
        if (_boardManager == null) return;

        BoardData board = _boardManager.CurrentBoard;
        if (board == null) return;

        var pair = FindAnyMatchablePair(board);
        if (pair.HasValue)
        {
            Debug.Log($"[DebugAutoPlayer] Hint: Có thể match {board.Cells[pair.Value.a].Value} ở [{pair.Value.a}] và {board.Cells[pair.Value.b].Value} ở [{pair.Value.b}]");
            
            // Tìm CellView tương ứng để animate
            var inputController = FindObjectOfType<InputController>();
            if (inputController != null)
            {
                // Thêm một chút effect rung nhẹ
                var rtA = _boardManager.transform.GetChild(0).GetChild(pair.Value.a).GetComponent<RectTransform>();
                var rtB = _boardManager.transform.GetChild(0).GetChild(pair.Value.b).GetComponent<RectTransform>();
                if (rtA != null) rtA.DOShakeScale(1f, 0.3f);
                if (rtB != null) rtB.DOShakeScale(1f, 0.3f);
            }
        }
        else
        {
            Debug.Log("[DebugAutoPlayer] Không còn nước đi nào!");
        }
    }

    // ─── MODE 1: Force Win ──────────────────────────────────────────────────
    /// <summary>
    /// Liên tục tìm cặp match được (kể cả gem) và match chúng.
    /// Khi không còn cặp nào → nếu cần vẫn còn Add thì Add thêm, rồi tiếp tục.
    /// Kết thúc khi Win popup xuất hiện.
    /// </summary>
    private IEnumerator RunForceWin()
    {
        _running = true;
        Debug.Log("[DebugAutoPlayer] 🏆 Force Win START");

        // Chờ 1 frame để BoardManager.Start() hoàn thành
        yield return null;

        while (_running)
        {
            BoardData board = _boardManager.CurrentBoard;
            if (board == null) { yield return new WaitForSeconds(0.2f); continue; }

            // Nếu đã win thì thoát
            if (board.IsAllGemsCollected())
            {
                Debug.Log("[DebugAutoPlayer] ✅ Đã thu đủ gem → Win!");
                break;
            }

            // Tìm cặp match được
            var pair = FindAnyMatchablePair(board);
            if (pair.HasValue)
            {
                // Giả lập click cell A rồi cell B thông qua InputController
                SimulateMatch(pair.Value.a, pair.Value.b);
                yield return new WaitForSeconds(_stepDelay);
            }
            else
            {
                // Không còn cặp: thử Add
                if (board.AddsRemaining > 0)
                {
                    Debug.Log($"[DebugAutoPlayer] Hết cặp → Add (còn {board.AddsRemaining})");
                    _boardManager.OnAddPressed();
                    yield return new WaitForSeconds(_stepDelay + 0.3f);
                }
                else
                {
                    Debug.LogWarning("[DebugAutoPlayer] Hết cặp + hết Add → không thể Win trong mode này.");
                    break;
                }
            }

            yield return null;
        }

        _running = false;
    }

    // ─── MODE 2: Force Lose ─────────────────────────────────────────────────
    /// <summary>
    /// Ưu tiên match các cặp KHÔNG phải gem trước (để GemCounter không tăng).
    /// Khi hết cặp non-gem → dùng hết lượt Add mà không match gì thêm.
    /// Khi AddsRemaining = 0 và không còn match → Lose popup.
    /// </summary>
    private IEnumerator RunForceLose()
    {
        _running = true;
        Debug.Log("[DebugAutoPlayer] 💀 Force Lose START");

        yield return null;

        while (_running)
        {
            BoardData board = _boardManager.CurrentBoard;
            if (board == null) { yield return new WaitForSeconds(0.2f); continue; }

            // Kiểm tra Lose condition
            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CheckLoseCondition(board))
            {
                Debug.Log("[DebugAutoPlayer] ✅ Điều kiện Lose đã đạt!");
                break;
            }

            // Tìm cặp non-gem trước
            var nonGemPair = FindNonGemMatchablePair(board);
            if (nonGemPair.HasValue)
            {
                SimulateMatch(nonGemPair.Value.a, nonGemPair.Value.b);
                yield return new WaitForSeconds(_stepDelay);
                continue;
            }

            // Không còn non-gem pair → dùng Add để thêm số và tạo cặp non-gem mới
            if (board.AddsRemaining > 0)
            {
                Debug.Log($"[DebugAutoPlayer] Dùng Add để về deadlock (còn {board.AddsRemaining})");
                _boardManager.OnAddPressed();
                yield return new WaitForSeconds(_stepDelay + 0.5f);
                continue;
            }

            // Hết Add + không còn pair nào (kể cả gem pair) → Lose
            var anyPair = FindAnyMatchablePair(board);
            if (!anyPair.HasValue)
            {
                Debug.Log("[DebugAutoPlayer] 💀 Deadlock đạt được → Lose!");
                break;
            }

            // Vẫn còn gem pair nhưng mình cố tình không match (chờ 1 frame)
            Debug.LogWarning("[DebugAutoPlayer] Vẫn còn pair là gem, hệ thống sẽ tự check Lose.");
            break;
        }

        _running = false;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private struct MatchPair { public int a; public int b; }

    /// <summary>Tìm bất kỳ cặp nào match được (kể cả gem).</summary>
    private MatchPair? FindAnyMatchablePair(BoardData board)
    {
        var unmatched = board.GetUnmatchedCells();
        for (int i = 0; i < unmatched.Count; i++)
        {
            for (int j = i + 1; j < unmatched.Count; j++)
            {
                if (MatchValidator.CanMatch(board, unmatched[i].Index, unmatched[j].Index))
                    return new MatchPair { a = unmatched[i].Index, b = unmatched[j].Index };
            }
        }
        return null;
    }

    /// <summary>Tìm cặp match được mà CẢ HAI đều không phải gem (ưu tiên cho ForceLose).</summary>
    private MatchPair? FindNonGemMatchablePair(BoardData board)
    {
        var unmatched = board.GetUnmatchedCells().Where(c => !c.IsGem).ToList();
        for (int i = 0; i < unmatched.Count; i++)
        {
            for (int j = i + 1; j < unmatched.Count; j++)
            {
                if (MatchValidator.CanMatch(board, unmatched[i].Index, unmatched[j].Index))
                    return new MatchPair { a = unmatched[i].Index, b = unmatched[j].Index };
            }
        }
        return null;
    }

    /// <summary>Gọi trực tiếp HandleMatchSuccess trên BoardManager thông qua InputController event chain.</summary>
    private void SimulateMatch(int idxA, int idxB)
    {
        BoardData board = _boardManager.CurrentBoard;
        if (board == null) return;

        // Validate lại trước khi match
        if (!MatchValidator.CanMatch(board, idxA, idxB))
        {
            Debug.LogWarning($"[DebugAutoPlayer] CanMatch({idxA},{idxB}) = false, bỏ qua.");
            return;
        }

        Debug.Log($"[DebugAutoPlayer] Match {board.Cells[idxA].Value}[{idxA}] ↔ {board.Cells[idxB].Value}[{idxB}]");

        // Fire event giống như InputController làm
        var inputController = _boardManager.GetComponentInChildren<InputController>();
        if (inputController == null) inputController = FindObjectOfType<InputController>();

        if (inputController != null)
        {
            // Dùng reflection để invoke private event OnMatchSuccess
            var eventField = typeof(InputController).GetField(
                "OnMatchSuccess",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            if (eventField != null)
            {
                var del = eventField.GetValue(inputController)
                    as System.Action<Cell, Cell>;
                del?.Invoke(board.Cells[idxA], board.Cells[idxB]);
            }
        }
    }
}
