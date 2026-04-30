using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System;

namespace NumMatch.Editor
{
    public class HardModeSolverWindow : EditorWindow
    {
        [MenuItem("NumMatch/Run Hard Mode Solver")]
        public static void RunSolver()
        {
            string dirPath = Application.streamingAssetsPath;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string inputPath = Path.Combine(dirPath, "input.txt");
            if (!File.Exists(inputPath))
            {
                // Create a sample 99-character input
                File.WriteAllText(inputPath, "123451234123451234123451234123451234123451234123451234123451234123451234123451234123451234123451234");
                UnityEngine.Debug.Log("[HardMode] Created sample input.txt in StreamingAssets");
            }

            string inputStr = File.ReadAllText(inputPath).Trim().Replace("\r", "").Replace("\n", "");
            if (string.IsNullOrEmpty(inputStr))
            {
                UnityEngine.Debug.LogError("[HardMode] input.txt is empty!");
                return;
            }

            HardModeSolver solver = new HardModeSolver();
            solver.Solve(inputStr);
        }
    }

    public class HardModeSolver
    {
        struct State : IEquatable<State>
        {
            public ulong mask0;
            public ulong mask1;

            public State(ulong m0, ulong m1)
            {
                mask0 = m0;
                mask1 = m1;
            }

            public bool Equals(State other) => mask0 == other.mask0 && mask1 == other.mask1;
            public override int GetHashCode() => unchecked((int)((mask0 ^ (mask0 >> 32)) ^ (mask1 ^ (mask1 >> 32))));
        }

        struct Move
        {
            public int idxA;
            public int idxB;
            public int gems;
        }

        private byte[] board;
        private int cols = 9;
        private int totalCells;
        private int targetGems;
        private int bestDepth = -1;
        private Dictionary<State, int> memo;
        private List<List<Move>> solutions;
        private Stopwatch sw;
        private long nodesExplored;
        private long prunedCount;
        private int maxSolutions = 10;
        private bool timeOut = false;
        private long timeBudgetMs;
        private ulong gemMask0;
        private ulong gemMask1;

        public void Solve(string inputStr)
        {
            totalCells = inputStr.Length;
            if (totalCells > 128)
            {
                UnityEngine.Debug.LogError("[HardMode] Input too large! Max 128 cells supported.");
                return;
            }

            board = new byte[totalCells];
            int totalGemsOnBoard = 0;
            gemMask0 = 0;
            gemMask1 = 0;
            for (int i = 0; i < totalCells; i++)
            {
                board[i] = (byte)(inputStr[i] - '0');
                if (board[i] == 5)
                {
                    totalGemsOnBoard++;
                    if (i < 64) gemMask0 |= 1UL << i;
                    else gemMask1 |= 1UL << (i - 64);
                }
            }

            targetGems = (totalGemsOnBoard % 2 == 1) ? ((totalGemsOnBoard / 2) * 2) : totalGemsOnBoard;
            bestDepth = -1;

            memo = new Dictionary<State, int>();
            solutions = new List<List<Move>>();
            sw = Stopwatch.StartNew();
            nodesExplored = 0;
            prunedCount = 0;
            timeOut = false;
            timeBudgetMs = totalCells >= 90 ? 5000L : 900L;

            State initialState = new State(0, 0);
            int initialLowerBound = (targetGems + 1) / 2;
            int maxDepth = totalCells / 2;

            for (int depthLimit = initialLowerBound; depthLimit <= maxDepth; depthLimit++)
            {
                memo.Clear();
                List<Move> currentPath = new List<Move>();
                int solutionsBefore = solutions.Count;
                bool exactDepthOnly = totalCells < 90 || bestDepth >= 0;
                DFS(initialState, 0, depthLimit, currentPath, exactDepthOnly);

                if (bestDepth < 0 && solutions.Count > solutionsBefore)
                {
                    bestDepth = depthLimit;
                }

                if (timeOut || solutions.Count >= maxSolutions)
                {
                    break;
                }
            }

            sw.Stop();

            UnityEngine.Debug.Log($"[HardMode] Best Depth: {bestDepth}, Solutions: {solutions.Count}, Time: {sw.ElapsedMilliseconds}ms / {timeBudgetMs}ms, Nodes Explored: {nodesExplored}, Pruned: {prunedCount}");

            WriteOutput();
        }

        private void DFS(State state, int movesUsed, int depthLimit, List<Move> path, bool exactDepthOnly)
        {
            if (timeOut || solutions.Count >= maxSolutions) return;

            if (sw.ElapsedMilliseconds > timeBudgetMs)
            {
                timeOut = true;
                return;
            }

            nodesExplored++;

            int gemsCollected = CountCollectedGems(state);
            if (gemsCollected >= targetGems)
            {
                if (!exactDepthOnly || movesUsed == depthLimit)
                {
                    solutions.Add(new List<Move>(path));
                }
                return;
            }

            int gemsRemaining = targetGems - gemsCollected;
            int lowerBound = (gemsRemaining + 1) / 2;

            if (movesUsed + lowerBound > depthLimit)
            {
                prunedCount++;
                return;
            }

            if (memo.TryGetValue(state, out int bestMoves))
            {
                bool shouldPrune = exactDepthOnly ? (movesUsed > bestMoves) : (movesUsed >= bestMoves);
                if (shouldPrune)
                {
                    prunedCount++;
                    return;
                }
            }
            memo[state] = movesUsed;

            List<Move> possibleMoves = GetValidMoves(state);

            // Pruning 1: Dead end detection (as per specification)
            // If we still need gems, but there is NO move that touches a gem on the board -> prune.
            if (gemsCollected < targetGems)
            {
                bool hasGemMove = false;
                for (int i = 0; i < possibleMoves.Count; i++)
                {
                    if (possibleMoves[i].gems > 0)
                    {
                        hasGemMove = true;
                        break;
                    }
                }
                if (!hasGemMove)
                {
                    prunedCount++;
                    return;
                }
            }

            // Pruning 3: Move ordering
            possibleMoves.Sort(CompareMoves);

            foreach (var move in possibleMoves)
            {
                State nextState = state;
                SetBit(ref nextState, move.idxA);
                SetBit(ref nextState, move.idxB);
                
                path.Add(move);
                DFS(nextState, movesUsed + 1, depthLimit, path, exactDepthOnly);
                path.RemoveAt(path.Count - 1);

                if (timeOut || solutions.Count >= maxSolutions) return;
            }
        }

        private int CompareMoves(Move a, Move b)
        {
            int gemCompare = b.gems.CompareTo(a.gems);
            if (gemCompare != 0) return gemCompare;

            int idxBCompare = b.idxB.CompareTo(a.idxB);
            if (idxBCompare != 0) return idxBCompare;

            return b.idxA.CompareTo(a.idxA);
        }

        private bool IsBitSet(State state, int idx)
        {
            if (idx < 64) return (state.mask0 & (1UL << idx)) != 0;
            else return (state.mask1 & (1UL << (idx - 64))) != 0;
        }

        private void SetBit(ref State state, int idx)
        {
            if (idx < 64) state.mask0 |= (1UL << idx);
            else state.mask1 |= (1UL << (idx - 64));
        }

        private int CountCollectedGems(State state)
        {
            return CountBits(state.mask0 & gemMask0) + CountBits(state.mask1 & gemMask1);
        }

        private int CountBits(ulong value)
        {
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        private List<Move> GetValidMoves(State state)
        {
            List<Move> moves = new List<Move>();
            int rows = (totalCells + cols - 1) / cols;

            for (int i = 0; i < totalCells; i++)
            {
                if (IsBitSet(state, i)) continue;
                byte val = board[i];

                int r = i / cols;
                int c = i % cols;

                // 1. Horizontal right on the same row
                for (int cc = c + 1; cc < cols; cc++)
                {
                    int j = r * cols + cc;
                    if (j >= totalCells) break;
                    if (!IsBitSet(state, j))
                    {
                        if (val == board[j] || val + board[j] == 10)
                        {
                            int gems = (val == 5 ? 1 : 0) + (board[j] == 5 ? 1 : 0);
                            moves.Add(new Move { idxA = i, idxB = j, gems = gems });
                        }
                        break;
                    }
                }

                // 2. Vertical Down
                for (int j = i + cols; j < totalCells; j += cols)
                {
                    if (!IsBitSet(state, j))
                    {
                        if (val == board[j] || val + board[j] == 10)
                        {
                            int gems = (val == 5 ? 1 : 0) + (board[j] == 5 ? 1 : 0);
                            moves.Add(new Move { idxA = i, idxB = j, gems = gems });
                        }
                        break;
                    }
                }

                // 3. Diagonal Down-Right
                for (int d = 1; r + d < rows && c + d < cols; d++)
                {
                    int j = (r + d) * cols + (c + d);
                    if (j >= totalCells) break;
                    if (!IsBitSet(state, j))
                    {
                        if (val == board[j] || val + board[j] == 10)
                        {
                            int gems = (val == 5 ? 1 : 0) + (board[j] == 5 ? 1 : 0);
                            moves.Add(new Move { idxA = i, idxB = j, gems = gems });
                        }
                        break;
                    }
                }

                // 4. Diagonal Down-Left
                for (int d = 1; r + d < rows && c - d >= 0; d++)
                {
                    int j = (r + d) * cols + (c - d);
                    if (j >= totalCells) break;
                    if (!IsBitSet(state, j))
                    {
                        if (val == board[j] || val + board[j] == 10)
                        {
                            int gems = (val == 5 ? 1 : 0) + (board[j] == 5 ? 1 : 0);
                            moves.Add(new Move { idxA = i, idxB = j, gems = gems });
                        }
                        break;
                    }
                }
            }
            return moves;
        }

        private void WriteOutput()
        {
            string outPath = Path.Combine(Application.streamingAssetsPath, "output.txt");
            using (StreamWriter writer = new StreamWriter(outPath))
            {
                foreach (var sol in solutions)
                {
                    List<string> parts = new List<string>();
                    foreach (var move in sol)
                    {
                        int r1 = move.idxA / cols;
                        int c1 = move.idxA % cols;
                        int r2 = move.idxB / cols;
                        int c2 = move.idxB % cols;
                        parts.Add($"{r1},{c1},{r2},{c2}");
                    }
                    writer.WriteLine(string.Join("|", parts));
                }
            }
            UnityEngine.Debug.Log($"[HardMode] Wrote {solutions.Count} solutions to {outPath}");
        }
    }
}
