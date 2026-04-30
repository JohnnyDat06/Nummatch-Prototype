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

        public static System.Collections.Generic.List<int> GetBlockingCells(BoardData b, int idxA, int idxB)
        {
            var blocking = new System.Collections.Generic.List<int>();
            if (idxA == idxB) return blocking;

            int min = Mathf.Min(idxA, idxB);
            int max = Mathf.Max(idxA, idxB);

            int rowA = min / b.Columns;
            int colA = min % b.Columns;
            int rowB = max / b.Columns;
            int colB = max % b.Columns;

            // Kiểm tra xem chúng thuộc loại đường nào.
            // Ưu tiên Horizontal/1D trước
            bool is1D = true;
            for (int i = min + 1; i < max; i++)
            {
                if (!b.Cells[i].IsMatched)
                {
                    blocking.Add(i);
                }
            }
            
            // Nếu chặn 1D nhưng không thuộc cùng cột hay chéo, ta trả về danh sách 1D.
            // Tuy nhiên, nếu chúng thẳng cột hoặc thẳng chéo, có thể user đang muốn match theo hướng đó,
            // ta nên lấy danh sách chặn theo hướng thẳng/chéo đó nếu nó ít hơn.
            
            var verticalBlocking = new System.Collections.Generic.List<int>();
            bool isVertical = (colA == colB);
            if (isVertical)
            {
                for (int i = min + b.Columns; i < max; i += b.Columns)
                {
                    if (!b.Cells[i].IsMatched) verticalBlocking.Add(i);
                }
            }

            var diagonalBlocking = new System.Collections.Generic.List<int>();
            bool isDiagonal = (Mathf.Abs(rowA - rowB) == Mathf.Abs(colA - colB));
            if (isDiagonal)
            {
                int step = (colA < colB) ? b.Columns + 1 : b.Columns - 1;
                for (int i = min + step; i < max; i += step)
                {
                    if (!b.Cells[i].IsMatched) diagonalBlocking.Add(i);
                }
            }

            // Chọn hướng hợp lý nhất: 
            // Nếu có hướng thẳng cột hoặc chéo và user có thể đang nhìn theo hướng đó, trả về danh sách đó.
            if (isVertical && verticalBlocking.Count > 0) return verticalBlocking;
            if (isDiagonal && diagonalBlocking.Count > 0) return diagonalBlocking;
            
            // Mặc định trả về 1D blocking nếu không có hướng đặc biệt nào
            return blocking;
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
