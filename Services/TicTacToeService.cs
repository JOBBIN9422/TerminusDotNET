using System;
using System.Collections.Generic;
using System.Linq;
using TerminusDotNetCore.Helpers;
using Discord;
using TerminusDotNetCore.Modules;
using System.Threading.Tasks;
using System.Text;

namespace TerminusDotNetCore.Services
{
    public enum BoardDirection
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    public enum Player
    {
        Player1,
        Player2
    }

    public class TicTacToeService : ICustomService
    {
        private int _winCount;
        public GameBoard Board { get; private set; }
        public bool GameActive { get; private set; }
        public int NumRows
        {
            get
            {
                return Board.NumRows;
            }
        }
        public int NumCols
        {
            get
            {
                return Board.NumCols;
            }
        }

        //make these readonly?
        public IUser Player1 { get; private set; }
        public IUser Player2 { get; private set; }
        public IUser NextPlayer { get; private set; }

        public ServiceControlModule ParentModule { get; set; }


        public TicTacToeService()
        {
            //Init(numRows, numCols, winCount, player1, player2);
        }

        public void Init(int numRows, int numCols, int winCount, IUser player1, IUser player2)
        {
            if (numRows <= 0 || numCols <= 0 || winCount <= 0)
            {
                throw new ArgumentException("The provided board dimensions/win count were invalid (must be greater than 0).");
            }
            int maxDiagLength = numRows < numCols ? numRows : numCols;
            if (winCount > maxDiagLength)
            {
                throw new ArgumentException("The provided win count is too large for the given board dimensions.");
            }
            else
            {
                Board = new GameBoard(numRows, numCols);
                _winCount = winCount;
                Player1 = player1;
                NextPlayer = player2;
                Player2 = player2;
                GameActive = true;
            }
        }

        public void EndGame()
        {
            Player1 = null;
            Player2 = null;
            NextPlayer = null;
            GameActive = false;
            Board = null;
        }

        public bool CheckTie()
        {
            foreach (var row in Board.State)
            {
                List<CellState> emptyCells = row.Where(cell => cell == CellState.Empty).ToList();
                if (emptyCells.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckWin(int startRow, int startCol)
        {
            //don't bother checking empty cells
            if (Board.State[startRow][startCol] == CellState.Empty)
            {
                return false;
            }

            List<int> winCounts = new List<int>();
            //count the number of pieces vertically
            winCounts.Add(CountDirection(startRow, startCol, BoardDirection.Up) + CountDirection(startRow, startCol, BoardDirection.Down) - 1);

            //count the number of pieces horizontally
            winCounts.Add(CountDirection(startRow, startCol, BoardDirection.Left) + CountDirection(startRow, startCol, BoardDirection.Right) - 1);

            //count the number of pieces for / diagonal
            winCounts.Add(CountDirection(startRow, startCol, BoardDirection.DownLeft) + CountDirection(startRow, startCol, BoardDirection.UpRight) - 1);

            //count the number of pieces for \ diagonal
            winCounts.Add(CountDirection(startRow, startCol, BoardDirection.DownRight) + CountDirection(startRow, startCol, BoardDirection.UpLeft) - 1);

            //do any of the above directions contain the proper number of pieces?
            return winCounts.Where(count => count == _winCount).ToList().Count > 0;
        }

        public async Task<bool> Place(IUser player, int row, int col)
        {
            //make sure that the proper player is placing 
            if (player != NextPlayer)
            {
                await ParentModule.ServiceReplyAsync($"{player.Username} is not the current player.");
                return false;
            }

            try
            {
                //can't place on occupied cell
                if (Board.State[row][col] != CellState.Empty)
                {
                    await ParentModule.ServiceReplyAsync($"Cannot play on space {row}, {col} (already played).");
                    return false;
                }

                if (NextPlayer == Player1)
                {
                    Board.State[row][col] = CellState.Player1;
                    NextPlayer = Player2;
                }
                else
                {
                    Board.State[row][col] = CellState.Player2;
                    NextPlayer = Player1;
                }

                if (CheckWin(row, col))
                {
                    await ParentModule.ServiceReplyAsync($"{player.Username} wins!");
                    GameActive = false;
                }

                if (CheckTie())
                {
                    await ParentModule.ServiceReplyAsync("It's a draw...");
                    GameActive = false;
                }

                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                await ParentModule.ServiceReplyAsync("Invalid move (out of bounds).");
                return false;
            }
        }

        public string GetBoardStateString()
        {
            StringBuilder sb = new StringBuilder("```", NumRows * NumCols * 2);
            sb.Append("  | ");
            for (int i = 0; i < Board.State[0].Count; i++)
            {
                sb.Append($"{i} ");
            }
            sb.AppendLine();
            sb.Append("--+-");

            for (int i = 0; i < Board.State[0].Count; i++)
            {
                sb.Append($"--");
            }
            sb.AppendLine();

            for (int i = 0; i < Board.State.Count; i++)
            {
                sb.Append($"{i} | ");
                for (int j = 0; j < Board.State[i].Count; j++)
                {
                    switch (Board.State[i][j])
                    {
                        case CellState.Empty:
                            sb.Append("- ");
                            break;

                        case CellState.Player1:
                            sb.Append("X ");
                            break;

                        case CellState.Player2:
                            sb.Append("O ");
                            break;
                    }
                }
                sb.AppendLine();
            }
            sb.AppendLine($"Next player: {NextPlayer.Username}\n```");

            return sb.ToString();
        }

        private int CountDirection(int startRow, int startCol, BoardDirection direction)
        {
            //what direction we'll be stepping in 
            int deltaRow;
            int deltaCol;

            switch (direction)
            {
                case BoardDirection.Up:
                    deltaRow = -1;
                    deltaCol = 0;
                    break;

                case BoardDirection.UpRight:
                    deltaRow = -1;
                    deltaCol = 1;
                    break;

                case BoardDirection.Right:
                    deltaRow = 0;
                    deltaCol = 1;
                    break;

                case BoardDirection.DownRight:
                    deltaRow = 1;
                    deltaCol = 1;
                    break;

                case BoardDirection.Down:
                    deltaRow = 1;
                    deltaCol = 0;
                    break;

                case BoardDirection.DownLeft:
                    deltaRow = 1;
                    deltaCol = -1;
                    break;

                case BoardDirection.Left:
                    deltaRow = 0;
                    deltaCol = -1;
                    break;

                case BoardDirection.UpLeft:
                    deltaRow = -1;
                    deltaCol = -1;
                    break;

                default:
                    deltaRow = 1;
                    deltaCol = 1;
                    break;
            }

            //determine who we're counting for
            CellState playerPiece = Board.State[startRow][startCol];
            int pieceCount = 0;

            int currRow = startRow;
            int currCol = startCol;

            try
            {
                while (Board.State[currRow][currCol] == playerPiece)
                {
                    pieceCount++;
                    currRow += deltaRow;
                    currCol += deltaCol;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                //prevent out-of-bounds errors (don't need to do anything here)
            }

            return pieceCount;
        }
    }
}
