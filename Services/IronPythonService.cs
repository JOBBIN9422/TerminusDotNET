using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.IO;

namespace TerminusDotNetCore.Services
{
    public class IronPythonService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }

        private ScriptEngine _pythonEngine = Python.CreateEngine();

        public List<string> ExecutePythonString(string pythonStr, int maxPageLength = 1990)
        {
            using (MemoryStream outputStream = new MemoryStream())
            using (StreamWriter outputWriter = new StreamWriter(outputStream))
            {
                //redirect std out to stream
                _pythonEngine.Runtime.IO.SetOutput(outputStream, outputWriter);

                //read in and execute the given code
                ScriptSource script = _pythonEngine.CreateScriptSourceFromString(pythonStr);
                script.Execute();

                //convert output stream to ASCII and reset std out 
                string output = Encoding.ASCII.GetString(outputStream.ToArray());
                _pythonEngine.Runtime.IO.SetOutput(Console.OpenStandardOutput(), Encoding.UTF8);

                //split output string into list of pages if it's too long
                List<string> outputPages = new List<string>();
                do
                {
                    if (output.Length > maxPageLength)
                    {
                        string outputPage = output.Substring(0, maxPageLength);
                        outputPages.Add(outputPage);
                        output = output.Substring(maxPageLength);
                    }
                    else
                    {
                        outputPages.Add(output);
                    }
                } while (output.Length > maxPageLength);

                //add any remaining output to the page list
                if (output.Length > 0)
                {
                    outputPages.Add(output);
                }

                return outputPages;
            }
        }
    }
}
