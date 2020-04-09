using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;
using System.Net.NetworkInformation;
using System.Net;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Helpers;
using System.IO;

namespace TerminusDotNetCore.Modules
{
    public class DndModule : ServiceControlModule
    {
        public DndModule(IConfiguration config) : base(config)
        {

        }

        private string error_message = "Dice count and size required. (Ex. !roll 2d10)";
        private string oblivion = "STOP! You have violated the law! (Check your input, too many or too few dice)";

        [Command("roll")]
        public async Task StonksAsync([Summary("Dice number and size.")]string dice_roll = null)
        {
            //Check for input to command
            if (string.IsNullOrEmpty(dice_roll))
            {
                await ServiceReplyAsync(error_message);
                await ServiceReplyAsync("Here is a completely random roll ya big idiot:");

                //Random Roll
                Random random = new Random();

                dice_roll = "1d" + random.Next(1000000).ToString();
            }
            
            string[] substrings = dice_roll.Split("d");

            //Check input format
            if (substrings.Length != 2)
            {
                await ServiceReplyAsync(error_message);
                return;
            }

            int roll_count = 0;
            int dice_amount = 0;
            int curr_roll = 0;
            int roll_sum = 0;
            int min_roll = 1000000;
            int max_roll = 0;

            //Convert roll amount and dice size to integers
            bool valid_roll_count = int.TryParse(substrings[0], out roll_count);
            bool valid_dice_amount = int.TryParse(substrings[1], out dice_amount);

            if (valid_roll_count == false || valid_dice_amount == false)
            {
                await ServiceReplyAsync(error_message);
                return;
            }

            if (roll_count <= 0 || roll_count > 100)
            {
                await ServiceReplyAsync(oblivion);
                return;
            }

            List<int> roll_list = new List<int>();

            //Commence rolling using DndHelper.DiceRoll
            for (int i = 0; i < roll_count; i++)
            {
                curr_roll = await DndHelper.RollDice(dice_amount);

                //Update maximum roll
                if (curr_roll > max_roll)
                {
                    max_roll = curr_roll;
                }

                if (curr_roll < min_roll)
                {
                    min_roll = curr_roll;
                }

                roll_list.Add(curr_roll);

                roll_sum = roll_sum + curr_roll;
            }

            //Print results
            await ServiceReplyAsync("**Results:** " + string.Join(", ", roll_list));

            if (roll_count > 1)
            {
                await ServiceReplyAsync("**Min:** " + min_roll.ToString());
                await ServiceReplyAsync("**Max:** " + max_roll.ToString());

                await ServiceReplyAsync("**Roll Sum:** " + roll_sum.ToString());
            }
        }
    }
}
