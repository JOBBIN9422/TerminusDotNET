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

        public List<string> ExecutePythonString(string pythonStr)
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
                while (output.Length > 2000)
                {
                    string outputPage = output.Substring(0, 2000);
                    outputPages.Add(outputPage);

                    if (output.Length > 2000)
                    {
                        output = output.Substring(2000);
                    }
                }

                return outputPages;
            }
        }
    }
}
