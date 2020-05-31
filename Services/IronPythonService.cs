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

        private ScriptEngine _pythonEngine;

        private MemoryStream _outputBinStream;
        private StreamWriter _outputTextStream;

        public IronPythonService(IConfiguration config)
        {
            Config = config;

            //init python eng and set output
            _outputBinStream = new MemoryStream();
            _outputTextStream = new StreamWriter(_outputBinStream);
            _pythonEngine = Python.CreateEngine();
            _pythonEngine.Runtime.IO.SetOutput(_outputBinStream, _outputTextStream);
        }

        public string ExecutePythonString(string pythonStr)
        {
            ScriptSource script = _pythonEngine.CreateScriptSourceFromString(pythonStr);
            script.Execute();
            string output = Encoding.ASCII.GetString(_outputBinStream.ToArray());
            return output;
        }
    }
}
