using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Property)]
    public class Description : Attribute
    {
        public string Text { get; }
        public Description(string text)
        {
            Text = text;
        }
    }
}
