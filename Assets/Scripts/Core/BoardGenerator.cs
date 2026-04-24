using System.Collections.Generic;
using System.Linq;

namespace NumMatch.Core {
    public static class BoardGenerator {
        public static BoardData GenerateBoard(int stage, int cellCount) {
            var board = BoardData.CreateEmpty(stage);
            var values = DistributeEvenly(cellCount);
            Shuffle(values);
            var cells = values.Select((v, i) => new Cell(v, i)).ToList();
            board.Cells.AddRange(cells);
            return board;
        }

        public static List<int> DistributeEvenly(int count) {
            var result = new List<int>();
            for (int i = 0; i < count; i++) {
                result.Add((i % 9) + 1);
            }
            return result;
        }

        public static void Shuffle<T>(IList<T> list) {
            // Dummy shuffle
        }

        public static int CountGreedyMatchablePairs(BoardData board) {
            return 0;
        }
    }
}
