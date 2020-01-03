using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Helpers;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    [Group("ttt")]
    public class TicTacToeModule : ServiceControlModule
    {
        private TicTacToeService _tttService;

        public TicTacToeModule(TicTacToeService service)
        {
            _tttService = service;
            _tttService.ParentModule = this;
        }

        [Command("new")]
        public async Task StartNewGame(IUser player2, int numRows = 3, int numCols = 3, int winCount = 3)
        {
            if (player2 == null)
            {
                throw new ArgumentException("Please challenge a @user when starting a new game.");
            }

            _tttService.Init(numRows, numCols, winCount, Context.Message.Author, player2);
            await ServiceReplyAsync(_tttService.GetBoardStateString());
        }

        [Command("place")]
        public async Task Place(int row, int col)
        {
            //if (row == null || col == null)
            //{
            //    throw new ArgumentException("Please provide a row and column number (e.g. 'place 1 1').");
            //}

            bool successfulPlay = await _tttService.Place(Context.Message.Author, row, col);

            if (successfulPlay)
            {
                await ServiceReplyAsync(_tttService.GetBoardStateString());
            }
        }
    }
}
