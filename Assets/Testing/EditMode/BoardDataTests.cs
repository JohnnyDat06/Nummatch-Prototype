using NUnit.Framework;
using NumMatch.Core;
using System.Collections.Generic;

namespace NumMatch.Testing.EditMode
{
    public class BoardDataTests
    {
        [Test]
        public void GetIndex_ConvertsCorrectly()
        {
            var board = BoardData.CreateEmpty(1);
            Assert.AreEqual(12, board.GetIndex(1, 3));
        }

        [Test]
        public void GetRow_ReturnsCorrectRow()
        {
            var board = BoardData.CreateEmpty(1);
            Assert.AreEqual(1, board.GetRow(12));
        }

        [Test]
        public void GetCol_ReturnsCorrectCol()
        {
            var board = BoardData.CreateEmpty(1);
            Assert.AreEqual(3, board.GetCol(12));
        }

        [Test]
        public void IsRowAllMatched_AllMatched_ReturnsTrue()
        {
            var board = BoardData.CreateFromValues(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 1);
            for (int i = 0; i < 9; i++) board.GetCell(i).IsMatched = true;
            Assert.IsTrue(board.IsRowAllMatched(0));
        }

        [Test]
        public void IsRowAllMatched_OneUnmatched_ReturnsFalse()
        {
            var board = BoardData.CreateFromValues(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 1);
            for (int i = 0; i < 8; i++) board.GetCell(i).IsMatched = true;
            Assert.IsFalse(board.IsRowAllMatched(0));
        }

        [Test]
        public void RemoveRow_ShiftsCellsUp()
        {
            var values = new List<int>();
            for (int i = 0; i < 18; i++) values.Add(i);
            var board = BoardData.CreateFromValues(values, 1);
            
            board.RemoveRow(0);
            
            Assert.AreEqual(9, board.Cells.Count);
            Assert.AreEqual(9, board.GetCell(0).Value); // The cell previously at index 9 is now at index 0
            Assert.AreEqual(0, board.GetCell(0).Index); // Ensure index is updated
        }

        [Test]
        public void GetUnmatchedCells_ReturnsOnlyUnmatched()
        {
            var board = BoardData.CreateFromValues(new List<int> { 1, 2, 3 }, 1);
            board.GetCell(1).IsMatched = true;
            
            var unmatched = board.GetUnmatchedCells();
            Assert.AreEqual(2, unmatched.Count);
            Assert.AreEqual(1, unmatched[0].Value);
            Assert.AreEqual(3, unmatched[1].Value);
        }

        [Test]
        public void AppendCells_ExtendsBoard()
        {
            var values = new List<int>();
            for (int i = 0; i < 27; i++) values.Add(i);
            var board = BoardData.CreateFromValues(values, 1);
            
            board.AppendCells(new List<int> { 1, 2, 3 });
            
            Assert.AreEqual(30, board.Cells.Count);
            Assert.AreEqual(27, board.GetCell(27).Index);
            Assert.AreEqual(28, board.GetCell(28).Index);
            Assert.AreEqual(29, board.GetCell(29).Index);
        }

        [Test]
        public void IsBoardAllMatched_AllMatched_ReturnsTrue()
        {
            var board = BoardData.CreateFromValues(new List<int> { 1, 2 }, 1);
            board.GetCell(0).IsMatched = true;
            board.GetCell(1).IsMatched = true;
            Assert.IsTrue(board.IsBoardAllMatched());
        }
    }
}
