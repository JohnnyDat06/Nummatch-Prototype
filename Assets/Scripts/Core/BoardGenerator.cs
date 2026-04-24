using System;
using System.Collections.Generic;
using System.Linq;

namespace NumMatch.Core {
    public static class BoardGenerator {
        
        private static Random rng = new Random();

        public static BoardData GenerateBoard(int stage, int cellCount) {
            int targetPairs = stage == 1 ? 3 : (stage == 2 ? 2 : 1);
            
            // Attempt purely random distributions up to 1500 times.
            for (int attempt = 0; attempt < 1500; attempt++) {
                var values = DistributeEvenly(cellCount);
                Shuffle(values);
                
                var board = BoardData.CreateEmpty(stage);
                board.Cells.AddRange(values.Select((v, i) => new Cell(v, i)).ToList());
                
                if (CountGreedyMatchablePairs(board) == targetPairs) {
                    return board;
                }
            }
            
            // Fallback: Random-Restart Hill-Climbing if pure random fails
            for (int restart = 0; restart < 10; restart++) {
                var fallbackValues = DistributeEvenly(cellCount);
                Shuffle(fallbackValues);
                var fallbackBoard = BoardData.CreateEmpty(stage);
                fallbackBoard.Cells.AddRange(fallbackValues.Select((v, i) => new Cell(v, i)).ToList());
                
                int currentPairs = CountGreedyMatchablePairs(fallbackBoard);
                if (currentPairs == targetPairs) return fallbackBoard;

                for (int swap = 0; swap < 5000; swap++) {
                    // Swap 2 random cells
                    int idx1 = rng.Next(cellCount);
                    int idx2 = rng.Next(cellCount);
                    if (idx1 == idx2) continue;
                    
                    int temp = fallbackBoard.Cells[idx1].Value;
                    fallbackBoard.Cells[idx1].Value = fallbackBoard.Cells[idx2].Value;
                    fallbackBoard.Cells[idx2].Value = temp;
                    
                    int newPairs = CountGreedyMatchablePairs(fallbackBoard);
                    
                    if (newPairs == targetPairs) return fallbackBoard;

                    // Accept if closer/equal, or occasionally (2%) to escape local optima
                    if (Math.Abs(newPairs - targetPairs) <= Math.Abs(currentPairs - targetPairs) || rng.NextDouble() < 0.02) {
                        currentPairs = newPairs;
                    } else {
                        // Revert
                        temp = fallbackBoard.Cells[idx1].Value;
                        fallbackBoard.Cells[idx1].Value = fallbackBoard.Cells[idx2].Value;
                        fallbackBoard.Cells[idx2].Value = temp;
                    }
                }
            }
            
            // Extreme fallback (should realistically never hit this in 50k swaps)
            return BoardData.CreateEmpty(stage);
        }

        public static List<int> DistributeEvenly(int count) {
            var result = new List<int>();
            for (int i = 0; i < count; i++) {
                result.Add((i % 9) + 1);
            }
            return result;
        }

        public static void Shuffle<T>(IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static int CountGreedyMatchablePairs(BoardData board) {
            int pairs = 0;
            var cells = board.Cells;
            List<int> temporarilyMatched = new List<int>();

            for (int i = 0; i < cells.Count; i++) {
                if (cells[i].IsMatched) continue; // Skip if conceptually matched already

                for (int j = i + 1; j < cells.Count; j++) {
                    if (cells[j].IsMatched) continue; // Skip if conceptually matched already

                    if (MatchValidator.CanMatch(board, i, j)) {
                        // Found a match => Greedily mark them as matched
                        cells[i].IsMatched = true;
                        cells[j].IsMatched = true;
                        temporarilyMatched.Add(i);
                        temporarilyMatched.Add(j);
                        pairs++;
                        break; // Stop looking for matches for 'i', move to next 'i'
                    }
                }
            }

            // Clean up: Revert the temporary match state before returning
            foreach (int idx in temporarilyMatched) {
                cells[idx].IsMatched = false;
            }

            return pairs;
        }
    }
}
