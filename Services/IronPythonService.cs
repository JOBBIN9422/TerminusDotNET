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
                _pythonEngine.Runtime.IO.SetOutput(outputStream, outputWriter);

                ScriptSource script = _pythonEngine.CreateScriptSourceFromString(pythonStr);
                script.Execute();

                string output = Encoding.ASCII.GetString(outputStream.ToArray());
                _pythonEngine.Runtime.IO.SetOutput(Console.OpenStandardOutput(), Encoding.UTF8);

                List<string> outputPages = new List<string>();
                do
                {
                    string outputPage = output.Substring(0, maxPageLength);
                    outputPages.Add(outputPage);

                    if (output.Length > maxPageLength)
                    {
                        output = output.Substring(maxPageLength);
                    }
                } while (output.Length > maxPageLength);

                return outputPages;
            }
        }
    }
}
