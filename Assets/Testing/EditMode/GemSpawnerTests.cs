using NUnit.Framework;
using NumMatch.Core;
using System.Collections.Generic;
using System.Linq;

namespace NumMatch.Testing.EditMode {
    public class GemSpawnerTests {

        private BoardData CreateTestBoard(int cellCount, int value = 1) {
            List<int> values = Enumerable.Repeat(value, cellCount).ToList();
            var board = BoardData.CreateFromValues(values, 1);
            board.GemsNeeded = new Dictionary<GemType, int>();
            board.GemsCollected = new Dictionary<GemType, int>();
            return board;
        }

        [Test]
        public void SpawnGems_WhenNoGemsNeeded_DoesNotSpawnAnyGem() {
            var board = CreateTestBoard(10);
            var newIndices = Enumerable.Range(0, 10).ToList();

            GemSpawner.SpawnGems(board, newIndices);

            int gemCount = board.Cells.Count(c => c.IsGem);
            Assert.AreEqual(0, gemCount, "Should not spawn gems if GemsNeeded is empty");
        }

        [Test]
        public void SpawnGems_WhenAllGemsCollected_DoesNotSpawnAnyGem() {
            var board = CreateTestBoard(10);
            board.GemsNeeded.Add(GemType.Orange, 1);
            board.GemsCollected.Add(GemType.Orange, 1); // Đã thu đủ
            var newIndices = Enumerable.Range(0, 10).ToList();

            GemSpawner.SpawnGems(board, newIndices);

            int gemCount = board.Cells.Count(c => c.IsGem);
            Assert.AreEqual(0, gemCount, "Should not spawn gems if all needed gems are collected");
        }

        [Test]
        public void SpawnGems_DoesNotExceed_Z_Cap() {
            // Setup 30 cells cho 1 lượt add
            var board = CreateTestBoard(30);
            
            // Cần 2 LOẠI ngọc -> Z = 2
            board.GemsNeeded.Add(GemType.Orange, 10);
            board.GemsNeeded.Add(GemType.Pink, 10);
            
            var newIndices = Enumerable.Range(0, 30).ToList();

            GemSpawner.SpawnGems(board, newIndices);

            int gemCount = board.Cells.Count(c => c.IsGem);
            Assert.LessOrEqual(gemCount, 2, "Gems spawned should never exceed Z (number of gem types needed)");
        }

        [Test]
        public void SpawnGems_Applies_Y_ChunkGuarantee_IfZAllows() {
            // Theo rule: Y = Ceil((n+1)/2).
            // Nếu n = 10, Y = Ceil(11/2) = 6.
            // Để Z không cản trở, setup Z > Y. Tuy GemType chỉ có 7 màu, ta fake 1 dictionary:
            var board = CreateTestBoard(10);
            board.GemsNeeded.Add(GemType.Orange, 1);
            board.GemsNeeded.Add(GemType.Pink, 1);
            board.GemsNeeded.Add(GemType.Red, 1);
            board.GemsNeeded.Add(GemType.Blue, 1);
            board.GemsNeeded.Add(GemType.Green, 1); // Z = 5

            var newIndices = Enumerable.Range(0, 10).ToList();

            GemSpawner.SpawnGems(board, newIndices);

            // Bắt buộc phải có ÍT NHẤT 1 gem mỗi cụm Y (6 cells). 
            // Với 10 cells, 6 cells đầu bắt buộc rải ít nhất 1 gem.
            int gemCount = board.Cells.Count(c => c.IsGem);
            Assert.GreaterOrEqual(gemCount, 1, "Must spawn at least 1 gem in the Y chunk");
        }

        [Test]
        public void SpawnGems_NewlySpawnedGems_CannotMatchEachOther() {
            int count = 100;
            // Dùng board siêu lớn để rải ngọc. Toàn bộ giá trị = 1 để kích thích tỷ lệ match cực cao.
            var board = CreateTestBoard(count, 1); 
            
            // Allow a massive number of gems to spawn by maximizing Z (số loại gem)
            // Vì thuật toán chặn maxZ = số loại gem, ta add đủ 7 loại Gem
            board.GemsNeeded.Add(GemType.Orange, 50);
            board.GemsNeeded.Add(GemType.Pink, 50);
            board.GemsNeeded.Add(GemType.Red, 50);
            board.GemsNeeded.Add(GemType.Blue, 50);
            board.GemsNeeded.Add(GemType.Green, 50);
            board.GemsNeeded.Add(GemType.Yellow, 50);
            board.GemsNeeded.Add(GemType.Purple, 50);

            var newIndices = Enumerable.Range(0, count).ToList();

            // Run gem spawner
            GemSpawner.SpawnGems(board, newIndices);

            var spawnedGems = board.Cells.Where(c => c.IsGem).ToList();
            
            // maxZ lúc này là 7, nên nó sẽ có thể rải ra tối đa 7 viên ngọc. 
            // Với 100 cells và tỷ lệ 5-7%, số ngọc chắc chắn >= 2 và lý thuyết có thể đạt tới 7.
            Assert.GreaterOrEqual(spawnedGems.Count, 2, "Should have spawned multiple gems for this large board");

            // Verify that NO TWO spawned gems can be matched with each other
            for (int i = 0; i < spawnedGems.Count; i++) {
                for (int j = i + 1; j < spawnedGems.Count; j++) {
                    int idxA = spawnedGems[i].Index;
                    int idxB = spawnedGems[j].Index;
                    
                    bool canMatch = MatchValidator.CanMatch(board, idxA, idxB);
                    Assert.IsFalse(canMatch, $"Gem at {idxA} and Gem at {idxB} should NOT be able to match, but they can!");
                }
            }
        }
    }
}
