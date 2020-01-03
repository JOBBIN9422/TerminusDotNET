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
        [Summary("Starts a new game with the given parameters.")]
        public async Task StartNewGame([Summary("The player to challenge.")]IUser player2, [Summary("Number of rows.")]int numRows = 3, [Summary("Number of columns.")]int numCols = 3, [Summary("How many pieces in a row count as a win.")]int winCount = 3)
        {
            if (player2 == null)
            {
                throw new ArgumentException("Please challenge a @user when starting a new game.");
            }

            _tttService.Init(numRows, numCols, winCount, Context.Message.Author, player2);
            await ServiceReplyAsync(_tttService.GetBoardStateString());
        }

        [Command("place")]
        [Summary("If you are the active player, place your piece at the specified location.")]
        public async Task Place([Summary("the zero-indexed row number to place at.")]int row, [Summary("the zero-indexed column to place at.")]int col)
        {
            //if (row == null || col == null)
            //{
            //    throw new ArgumentException("Please provide a row and column number (e.g. 'place 1 1').");
            //}

            if (!_tttService.GameActive)
            {
                await ServiceReplyAsync("No game is currently active.");
                return;
            }
            bool successfulPlay = await _tttService.Place(Context.Message.Author, row, col);

            if (successfulPlay)
            {
                await ServiceReplyAsync(_tttService.GetBoardStateString());
            }
        }

        [Command("show")]
        [Summary("Print the current board and next player to the chat.")]
        public async Task ShowBoard()
        {
            if (!_tttService.GameActive)
            {
                await ServiceReplyAsync("No game is currently active.");
                return;
            }
            await ServiceReplyAsync(_tttService.GetBoardStateString());
        }

        [Command("resign")]
        [Summary("If you are the active player, quit the game.")]
        public async Task Quit()
        {
            if (!_tttService.GameActive)
            {
                await ServiceReplyAsync("No game is currently active.");
                return;
            }

            if (Context.Message.Author != _tttService.NextPlayer)
            {
                await ServiceReplyAsync($"Only the active player ({_tttService.NextPlayer.Username}) can forfeit the game.");
            }
            else
            {
                _tttService.EndGame();
                await ServiceReplyAsync($"{Context.Message.Author.Username} has resigned.");
            }
        }
    }
}
