using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace TerminusDotNetCore.Services
{
    public class IronPythonService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }

        private ScriptEngine _pythonEngine = Python.CreateEngine();

        public string ExecutePythonString(string pythonStr)
        {
            ScriptSource script = _pythonEngine.CreateScriptSourceFromString(pythonStr);
            string output = script.Execute();

            return output;
        }
    }
}
