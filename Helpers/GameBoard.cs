using System;
using System.Collections.Generic;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public enum CellState
    {
        Empty,
        Player1,
        Player2
    }

    public class GameBoard
    {
        public List<List<CellState>> State { get; private set; }

        public int NumRows
        {
            get
            {
                return State.Count;
            }
        }

        public int NumCols
        {
            get
            {
                return State[0].Count;
            }
        }

        public GameBoard(int numRows, int numCols)
        {
            State = new List<List<CellState>>();
            for (int y = 0; y < numRows; y++)
            {
                List<CellState> currRow = new List<CellState>();
                for (int x = 0; x < numCols; x++)
                {
                    currRow.Add(CellState.Empty);
                }
                State.Add(currRow);
            }
        }
    }
}
