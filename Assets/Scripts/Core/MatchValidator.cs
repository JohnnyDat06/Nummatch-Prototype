using UnityEngine;

namespace NumMatch.Core
{
    public static class MatchValidator
    {
        public static bool IsBlocked(BoardData b, int idxA, int idxB)
        {
            if (idxA == idxB) return true;

            int min = Mathf.Min(idxA, idxB);
            int max = Mathf.Max(idxA, idxB);

            // 1. Horizontal / 1D Linear check
            bool horizontalBlocked = false;
            for (int i = min + 1; i < max; i++)
            {
                if (!b.Cells[i].IsMatched)
                {
                    horizontalBlocked = true;
                    break;
                }
            }
            if (!horizontalBlocked) return false;

            int rowA = min / b.Columns;
            int colA = min % b.Columns;
            int rowB = max / b.Columns;
            int colB = max % b.Columns;

            // 2. Vertical check
            if (colA == colB)
            {
                bool verticalBlocked = false;
                for (int i = min + b.Columns; i < max; i += b.Columns)
                {
                    if (!b.Cells[i].IsMatched)
                    {
                        verticalBlocked = true;
                        break;
                    }
                }
                if (!verticalBlocked) return false;
            }

            // 3. Diagonal check
            if (Mathf.Abs(rowA - rowB) == Mathf.Abs(colA - colB))
            {
                bool diagonalBlocked = false;
                int step = (colA < colB) ? b.Columns + 1 : b.Columns - 1; // 10 is ↘, 8 is ↙ (assuming columns=9)
                
                for (int i = min + step; i < max; i += step)
                {
                    if (!b.Cells[i].IsMatched)
                    {
                        diagonalBlocked = true;
                        break;
                    }
                }
                if (!diagonalBlocked) return false;
            }

            return true;
        }

        public static bool CanMatch(BoardData b, int idxA, int idxB)
        {
            if (idxA == idxB) return false;
            
            if (idxA < 0 || idxA >= b.Cells.Count || idxB < 0 || idxB >= b.Cells.Count) return false;
            
            Cell cellA = b.Cells[idxA];
            Cell cellB = b.Cells[idxB];
            
            if (cellA.IsMatched || cellB.IsMatched) return false;
            
            bool isValidValue = (cellA.Value == cellB.Value) || (cellA.Value + cellB.Value == 10);
            if (!isValidValue) return false;
            
            return !IsBlocked(b, idxA, idxB);
        }

        public static bool HasAnyMatchablePair(BoardData b)
        {
            var unmatched = b.GetUnmatchedCells();
            for (int i = 0; i < unmatched.Count; i++)
            {
                for (int j = i + 1; j < unmatched.Count; j++)
                {
                    if (CanMatch(b, unmatched[i].Index, unmatched[j].Index))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
