using NUnit.Framework;
using NumMatch.Core;

namespace NumMatch.Testing.EditMode
{
    public class MatchValidatorTests
    {
        private BoardData CreateBoard(int cellsCount, params int[] matchedIndices)
        {
            var board = new BoardData { Columns = 9 };
            for (int i = 0; i < cellsCount; i++)
            {
                board.Cells.Add(new Cell { Index = i, Value = 1, IsMatched = false });
            }
            foreach (var idx in matchedIndices)
            {
                board.Cells[idx].IsMatched = true;
            }
            return board;
        }

        [Test]
        public void IsBlocked_AdjacentHorizontal_ReturnsFalse()
        {
            var board = CreateBoard(27);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 1));
        }

        [Test]
        public void IsBlocked_AdjacentVertical_ReturnsFalse()
        {
            var board = CreateBoard(27);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 9));
        }

        [Test]
        public void IsBlocked_AdjacentDiagonal_ReturnsFalse()
        {
            var board = CreateBoard(27);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 10)); // ↘
            Assert.IsFalse(MatchValidator.IsBlocked(board, 1, 9));  // ↙
        }

        [Test]
        public void IsBlocked_WrapAroundEndRowNToStartRowNPlus1_ReturnsFalse()
        {
            // Cuối hàng 0 (index 8) và đầu hàng 1 (index 9)
            var board = CreateBoard(27);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 8, 9));
        }

        [Test]
        public void IsBlocked_WithMatchedCellBetweenHorizontal_ReturnsFalse()
        {
            var board = CreateBoard(27, 1); // cell 1 is matched
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 2));
        }

        [Test]
        public void IsBlocked_WithUnmatchedCellBetweenHorizontal_ReturnsTrue()
        {
            var board = CreateBoard(27); // cell 1 is unmatched
            Assert.IsTrue(MatchValidator.IsBlocked(board, 0, 2));
        }

        [Test]
        public void IsBlocked_WithMatchedCellBetweenVertical_ReturnsFalse()
        {
            var board = CreateBoard(27, 9); // row 1 col 0 is matched
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 18));
        }

        [Test]
        public void IsBlocked_WithUnmatchedCellBetweenVertical_ReturnsTrue()
        {
            var board = CreateBoard(27); 
            Assert.IsTrue(MatchValidator.IsBlocked(board, 0, 18));
        }

        [Test]
        public void IsBlocked_WithMatchedCellBetweenDiagonal_ReturnsFalse()
        {
            var board = CreateBoard(27, 10); // row 1 col 1 matched
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 20)); // row 0 col 0 to row 2 col 2
        }

        [Test]
        public void IsBlocked_WithUnmatchedCellBetweenDiagonal_ReturnsTrue()
        {
            var board = CreateBoard(27); 
            Assert.IsTrue(MatchValidator.IsBlocked(board, 0, 20));
        }

        [Test]
        public void IsBlocked_SameCell_ReturnsTrue()
        {
            var board = CreateBoard(27);
            Assert.IsTrue(MatchValidator.IsBlocked(board, 5, 5));
        }

        [Test]
        public void IsBlocked_DiagonalThroughMultipleMatchedCells_ReturnsFalse()
        {
            var board = CreateBoard(36, 10, 20); // ↘ through (1,1) and (2,2)
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 30)); // (0,0) to (3,3)
        }

        [Test]
        public void IsBlocked_NotOnSameLineOrDiagonal_ReturnsTrue()
        {
            var board = CreateBoard(27);
            Assert.IsTrue(MatchValidator.IsBlocked(board, 0, 11)); // (0,0) to (1,2)
        }

        [Test]
        public void IsBlocked_HorizontalPathUnblockedEvenIfNotSameRow_ReturnsFalse()
        {
            // Index 0 to 11. Cells 1..10 are matched.
            var board = CreateBoard(27, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 0, 11)); // Passed through wrap!
        }

        [Test]
        public void CanMatch_ValidValues_ReturnsTrue()
        {
            var board = CreateBoard(27);
            board.Cells[0].Value = 5;
            board.Cells[1].Value = 5;
            Assert.IsTrue(MatchValidator.CanMatch(board, 0, 1));

            board.Cells[0].Value = 4;
            board.Cells[1].Value = 6;
            Assert.IsTrue(MatchValidator.CanMatch(board, 0, 1));
        }

        [Test]
        public void CanMatch_InvalidValues_ReturnsFalse()
        {
            var board = CreateBoard(27);
            board.Cells[0].Value = 3;
            board.Cells[1].Value = 5;
            Assert.IsFalse(MatchValidator.CanMatch(board, 0, 1));
        }

        [Test]
        public void CanMatch_SelectMatchedCell_ReturnsFalse()
        {
            var board = CreateBoard(27);
            board.Cells[0].Value = 5;
            board.Cells[1].Value = 5;
            board.Cells[0].IsMatched = true; // One cell is already matched
            Assert.IsFalse(MatchValidator.CanMatch(board, 0, 1));
        }

        [Test]
        public void IsBlocked_WrapAroundWithMultipleRows_ReturnsFalse()
        {
            // from index 2 to index 17. 3 through 16 are matched.
            var board = CreateBoard(27, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Assert.IsFalse(MatchValidator.IsBlocked(board, 2, 17));
        }
    }
}
