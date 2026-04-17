using System.Collections.Generic;
using UnityEngine;

namespace NumMatch.Core
{
    public class BoardData
    {
        public List<Cell> Cells = new List<Cell>();        // 1D ARRAY
        public int Columns = 9;
        public int Rows => Mathf.CeilToInt((float)Cells.Count / Columns);
        public int Stage;
        public int AddsRemaining;
        public Dictionary<GemType, int> GemsNeeded = new Dictionary<GemType, int>();
        public Dictionary<GemType, int> GemsCollected = new Dictionary<GemType, int>();
    }
}
