using System;
using UnityEngine;

namespace CityPuzzle.Puzzle
{
    public class PuzzleBoard : MonoBehaviour
    {
        public event Action OnCompleted;

        int totalPieces;
        int snappedPieces;

        public void Initialize(int pieceCount)
        {
            totalPieces = pieceCount;
            snappedPieces = 0;
        }

        public void NotifyPieceSnapped()
        {
            snappedPieces++;
            if (snappedPieces >= totalPieces)
            {
                OnCompleted?.Invoke();
            }
        }
    }
}
