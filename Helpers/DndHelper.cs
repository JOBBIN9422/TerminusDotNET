using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.IO;

namespace TerminusDotNetCore.Helpers
{
    class DndHelper
    {
        public static int RollDice(int die)
        {
            Random random = new Random();
            int roll_result = random.Next(1, die+1);

            return roll_result;
        }
    }
}