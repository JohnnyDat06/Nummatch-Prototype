using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NumMatch.Core;
using UnityEngine;

namespace NumMatch.Tests
{
    [TestFixture]
    public class BoardGeneratorTests
    {
        // Helper method to create a board manually for specific test cases
        private BoardData MakeBoard(int[] values)
        {
            var cells = values.Select((v, i) => new Cell(v, i) { IsMatched = false }).ToList();
            var board = BoardData.CreateEmpty(1);
            board.Cells.AddRange(cells);
            return board;
        }

        #region T18 - Phân phối đều các số 1-9 (chênh lệch max 1)

        [Test]
        public void Distribute_27Cells_EachValueAppears3Times()
        {
            // 27 / 9 = 3 đều tuyệt đối, chênh lệch = 0
            var cells = BoardGenerator.DistributeEvenly(27);
            for (int v = 1; v <= 9; v++)
            {
                Assert.AreEqual(3, cells.Count(c => c == v));
            }
        }

        [Test]
        public void Distribute_28Cells_MaxDiffIsOne()
        {
            // 28 = 9*3 + 1 → một số có 4 lần, còn lại 3 lần
            var cells = BoardGenerator.DistributeEvenly(28);
            int max = cells.GroupBy(c => c).Max(g => g.Count());
            int min = cells.GroupBy(c => c).Min(g => g.Count());
            Assert.LessOrEqual(max - min, 1);
        }

        [Test]
        public void Distribute_41Cells_MaxDiffIsOne()
        {
            // 41 = 9*4 + 5 → pattern [5,4,5,5,4,5,5,4,5] như spec
            var cells = BoardGenerator.DistributeEvenly(41);
            int max = cells.GroupBy(c => c).Max(g => g.Count());
            int min = cells.GroupBy(c => c).Min(g => g.Count());
            Assert.LessOrEqual(max - min, 1);
            Assert.AreEqual(41, cells.Count);
        }

        [Test]
        public void Distribute_AnyCount_TotalIsCorrect([Values(27, 36, 41, 54, 100)] int count)
        {
            var cells = BoardGenerator.DistributeEvenly(count);
            Assert.AreEqual(count, cells.Count);
        }

        [Test]
        public void Distribute_AnyCount_AllValuesPresent([Values(27, 36, 41)] int count)
        {
            var cells = BoardGenerator.DistributeEvenly(count);
            for (int v = 1; v <= 9; v++)
            {
                Assert.IsTrue(cells.Any(c => c == v), $"Value {v} missing in {count} cells");
            }
        }

        #endregion

        #region T19 - Đủ số từ 1-9 trong mỗi stage

        [Test]
        public void Generate_Stage1_ContainsAllValues1To9()
        {
            var board = BoardGenerator.GenerateBoard(stage: 1, cellCount: 27);
            for (int v = 1; v <= 9; v++)
            {
                Assert.IsTrue(board.Cells.Any(c => c.Value == v && !c.IsMatched), $"Stage 1 missing value {v}");
            }
        }

        [TestCase(1, 27)]
        [TestCase(2, 27)]
        [TestCase(3, 27)]
        public void Generate_AnyStage_ContainsAllValues(int stage, int cellCount)
        {
            var board = BoardGenerator.GenerateBoard(stage, cellCount);
            for (int v = 1; v <= 9; v++)
            {
                Assert.IsTrue(board.Cells.Any(c => c.Value == v), $"Stage {stage} missing value {v}");
            }
        }

        [Test]
        public void Generate_AfterShuffle_StillContainsAllValues()
        {
            // Shuffle không được làm mất giá trị
            var cells = BoardGenerator.DistributeEvenly(27);
            BoardGenerator.Shuffle(cells);
            for (int v = 1; v <= 9; v++)
            {
                Assert.IsTrue(cells.Any(c => c == v), $"Shuffle lost value {v}");
            }
        }

        #endregion

        #region T20 - Đúng số cặp match ban đầu

        [Test]
        public void CountPairs_Stage1_Returns3()
        {
            // Chạy 20 lần để đảm bảo không phải may mắn
            int failCount = 0;
            for (int i = 0; i < 20; i++)
            {
                var board = BoardGenerator.GenerateBoard(stage: 1, cellCount: 27);
                int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
                if (pairs != 3) failCount++;
            }
            Assert.AreEqual(0, failCount, $"Stage 1 failed {failCount}/20 times");
        }

        [Test]
        public void CountPairs_Stage2_Returns2()
        {
            int failCount = 0;
            for (int i = 0; i < 20; i++)
            {
                var board = BoardGenerator.GenerateBoard(stage: 2, cellCount: 27);
                int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
                if (pairs != 2) failCount++;
            }
            Assert.AreEqual(0, failCount, $"Stage 2 failed {failCount}/20 times");
        }

        [Test]
        public void CountPairs_Stage3Plus_Returns1()
        {
            int failCount = 0;
            for (int i = 0; i < 20; i++)
            {
                var board = BoardGenerator.GenerateBoard(stage: 3, cellCount: 27);
                int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
                if (pairs != 1) failCount++;
            }
            Assert.AreEqual(0, failCount, $"Stage 3 failed {failCount}/20 times");
        }

        [Test]
        public void GenerateBoardTester_1000Runs_Stage1_NoFail()
        {
            // Editor stress test — log histogram
            int fail = 0;
            var histogram = new Dictionary<int, int>();
            for (int i = 0; i < 1000; i++)
            {
                var board = BoardGenerator.GenerateBoard(stage: 1, cellCount: 27);
                int p = BoardGenerator.CountGreedyMatchablePairs(board);
                
                if (!histogram.ContainsKey(p)) histogram[p] = 0;
                histogram[p]++;
                
                if (p != 3) fail++;
            }
            
            // Log histogram để debug nếu fail
            foreach (var kv in histogram)
            {
                Debug.Log($"pairs={kv.Key}: {kv.Value} lần");
            }
            Assert.AreEqual(0, fail, $"Stage 1: {fail}/1000 boards sai số cặp");
        }

        #endregion

        #region T21 - Rule "1 số match 2 hướng = chỉ tính 1 cặp" trong validate

        [Test]
        public void GreedyCount_CellMatchable2Directions_CountsOnlyOnce()
        {
            // Layout 1×9: [5, 5, 5, 1, 2, 3, 4, 6, 7]
            // Cell[0]=5 có thể match Cell[1]=5 (liền kề ngang)
            // Cell[1]=5 có thể match Cell[2]=5 (liền kề ngang)
            // Greedy: match (0,1) → Cell[2] không còn pair → pairs = 1
            var board = MakeBoard(new[] { 5, 5, 5, 1, 2, 3, 4, 6, 7 });
            int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
            Assert.AreEqual(1, pairs, "3 số 5 liên tiếp phải tính là 1 cặp greedy, không phải 2");
        }

        [Test]
        public void GreedyCount_CellA_MatchesB_And_C_CountsOnce()
        {
            // Cell A vừa match được B (ngang) vừa match được C (dọc)
            // Sau khi greedy chọn (A,B) → C không còn pair với A
            // Tổng = 1 cặp, không phải 2
            // Board 2×9:
            // row0: [5, 5, 1, 2, 3, 4, 6, 7, 8]
            // row1: [5, 9, 1, 2, 3, 4, 6, 7, 8]
            // Cell[0]=5: match ngang Cell[1]=5, match dọc Cell[9]=5
            var board = MakeBoard(new[] {
                5, 5, 1, 2, 3, 4, 6, 7, 8,
                5, 9, 1, 2, 3, 4, 6, 7, 8
            });
            int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
            // Greedy chọn (0,1) → Cell[9] không còn match → tổng = 1
            Assert.AreEqual(1, pairs, "Cell match 2 hướng chỉ được tính 1 cặp");
        }

        [Test]
        public void GreedyCount_TwoIndependentPairs_CountsTwo()
        {
            // Xác nhận greedy vẫn đếm đúng khi có 2 cặp độc lập
            // row0: [1, 1, 2, 3, 4, 5, 6, 7, 8]
            // row1: [9, 9, 2, 3, 4, 5, 6, 7, 8]
            // (0,1)=pair1 độc lập, (9,10)=pair2 độc lập
            var board = MakeBoard(new[] {
                1, 1, 2, 3, 4, 5, 6, 7, 8,
                9, 9, 2, 3, 4, 5, 6, 7, 8
            });
            int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
            Assert.AreEqual(2, pairs, "2 cặp độc lập phải count đúng là 2");
        }

        [Test]
        public void GreedyCount_NoMatchablePair_ReturnsZero()
        {
            // Board không có cặp nào match được
            // Tất cả cell đều bị chặn hoặc không có value hợp lệ
            var board = MakeBoard(new[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9
            });
            // 1 hàng, không cell nào match nhau (không cùng value, không tổng=10)
            // Thực ra 1+9=10 → (0,8) match nhưng bị chặn bởi 7 cell giữa chưa matched
            int pairs = BoardGenerator.CountGreedyMatchablePairs(board);
            Assert.AreEqual(0, pairs, "Không có cặp nào không bị chặn → phải trả về 0");
        }

        #endregion
    }
}
