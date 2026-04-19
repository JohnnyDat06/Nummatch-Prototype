namespace NumMatch.Core
{
    public class Cell
    {
        public int Value;      // 1-9
        public int Index;
        public bool IsMatched;
        public bool IsGem;
        public GemType GemColor;

        public Cell(int value, int index)
        {
            Value = value;
            Index = index;
            Reset();
        }

        public void Reset()
        {
            IsMatched = false;
            IsGem = false;
            GemColor = GemType.None;
        }

        public override string ToString()
        {
            return IsMatched ? $"[{Value}*]" : $"[{Value}]";
        }
    }
}
