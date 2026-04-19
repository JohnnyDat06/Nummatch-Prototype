using System.Collections.Generic;
using System.Linq;

namespace NumMatch.Core
{
    public class BoardData
    {
        public List<Cell> Cells { get; private set; } = new List<Cell>();
        public int Columns { get; } = 9;
        public int Rows => (int)System.Math.Ceiling((double)Cells.Count / Columns);
        public int Stage { get; set; }
        public int AddsRemaining { get; set; }
        public Dictionary<GemType, int> GemsNeeded { get; set; } = new Dictionary<GemType, int>();
        public Dictionary<GemType, int> GemsCollected { get; set; } = new Dictionary<GemType, int>();

        /// <summary>Convert row/col to 1D index.</summary>
        public int GetIndex(int row, int col) => row * Columns + col;

        /// <summary>Get row from 1D index.</summary>
        public int GetRow(int index) => index / Columns;

        /// <summary>Get col from 1D index.</summary>
        public int GetCol(int index) => index % Columns;

        /// <summary>Get cell by row and col.</summary>
        public Cell GetCell(int row, int col)
        {
            int index = GetIndex(row, col);
            return GetCell(index);
        }

        /// <summary>Get cell by 1D index.</summary>
        public Cell GetCell(int index)
        {
            if (IsValidIndex(index))
                return Cells[index];
            return null;
        }

        /// <summary>Check if index is within bounds.</summary>
        public bool IsValidIndex(int index) => index >= 0 && index < Cells.Count;

        /// <summary>Get all cells that are not matched yet.</summary>
        public List<Cell> GetUnmatchedCells()
        {
            return Cells.Where(c => !c.IsMatched).ToList();
        }

        /// <summary>Check if all cells in a specific row are matched.</summary>
        public bool IsRowAllMatched(int row)
        {
            int startIndex = row * Columns;
            int endIndex = System.Math.Min(startIndex + Columns, Cells.Count);
            
            if (startIndex >= Cells.Count) return false; // Row out of bounds

            for (int i = startIndex; i < endIndex; i++)
            {
                if (!Cells[i].IsMatched) return false;
            }
            return true;
        }

        /// <summary>Remove a row and shift all subsequent cells up (recalculating indices).</summary>
        public void RemoveRow(int row)
        {
            int startIndex = row * Columns;
            int endIndex = startIndex + Columns;

            if (startIndex >= Cells.Count) return;

            int cellsToRemoveCount = System.Math.Min(Columns, Cells.Count - startIndex);
            Cells.RemoveRange(startIndex, cellsToRemoveCount);

            // Update indices after removal
            for (int i = startIndex; i < Cells.Count; i++)
            {
                Cells[i].Index = i;
            }
        }

        /// <summary>Append new cells to the end of the board based on a sequence of values.</summary>
        public void AppendCells(IEnumerable<int> values)
        {
            foreach (var value in values)
            {
                Cells.Add(new Cell(value, Cells.Count));
            }
        }

        /// <summary>Check if the entire board is cleared (all matched).</summary>
        public bool IsBoardAllMatched()
        {
            if (Cells.Count == 0) return true;
            return Cells.All(c => c.IsMatched);
        }

        /// <summary>Check if all required gems have been collected to win the stage.</summary>
        public bool IsAllGemsCollected()
        {
            if (GemsNeeded == null || GemsNeeded.Count == 0) return false;
            foreach (var kvp in GemsNeeded)
            {
                if (!GemsCollected.ContainsKey(kvp.Key) || GemsCollected[kvp.Key] < kvp.Value)
                    return false;
            }
            return true;
        }

        /// <summary>Factory method to create an empty board for a stage.</summary>
        public static BoardData CreateEmpty(int stage)
        {
            return new BoardData
            {
                Stage = stage
            };
        }

        /// <summary>Factory method to create a board pre-populated with values.</summary>
        public static BoardData CreateFromValues(List<int> values, int stage)
        {
            var board = new BoardData
            {
                Stage = stage
            };
            board.AppendCells(values);
            return board;
        }
    }
}
