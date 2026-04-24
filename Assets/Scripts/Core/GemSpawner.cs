using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NumMatch.Core {
    public static class GemSpawner {
        
        public static void SpawnGems(BoardData board, List<int> newCellIndices) {
            if (newCellIndices == null || newCellIndices.Count == 0) return;
            
            float xRate = Random.Range(0.05f, 0.07f);
            int yChunkStr = Mathf.CeilToInt((newCellIndices.Count + 1) / 2f);
            
            var availableTypes = board.GemsNeeded.Keys
                .Where(t => !board.GemsCollected.ContainsKey(t) || board.GemsCollected[t] < board.GemsNeeded[t])
                .ToList();
                
            int maxZ = availableTypes.Count;
            if (maxZ == 0) return; // Gems fulfilled
            
            int spawnedObjCount = 0;
            int yCounter = 0;
            List<int> spawnedIdx = new List<int>();
            
            foreach (int idx in newCellIndices) {
                if (spawnedObjCount >= maxZ) break;
                
                bool shouldSpawn = Random.value < xRate || yCounter >= yChunkStr - 1;
                
                if (shouldSpawn) {
                    if (CanMatchAnyGemIn(idx, spawnedIdx, board)) {
                        yCounter++;
                        continue;
                    }
                    
                    var cell = board.Cells[idx];
                    cell.IsGem = true;
                    // Pick random from available types
                    cell.GemColor = availableTypes[Random.Range(0, availableTypes.Count)];
                    
                    spawnedIdx.Add(idx);
                    spawnedObjCount++;
                    yCounter = 0;
                } else {
                    yCounter++;
                }
            }
        }
        
        private static bool CanMatchAnyGemIn(int newGemIdx, List<int> currentSpawnedGems, BoardData board) {
            foreach (int otherIdx in currentSpawnedGems) {
                if (MatchValidator.CanMatch(board, newGemIdx, otherIdx)) {
                    return true;
                }
            }
            return false;
        }
    }
}
