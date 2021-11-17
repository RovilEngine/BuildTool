using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace RobloxScriptCompiler
{
    class LuaInterop
    {
        private Random rand = new Random();
        private string bindir = Directory.GetCurrentDirectory() + "\\bin";
        private string tmpdir = Directory.GetCurrentDirectory() + "\\tmp";
        public LuaInterop()
        {
            if (!Directory.Exists(bindir)) Directory.CreateDirectory(bindir);
            if (Directory.Exists(tmpdir))
            {
                foreach (FileInfo file in new DirectoryInfo(tmpdir).GetFiles())
                {
                    file.Delete();
                }
            } else Directory.CreateDirectory(tmpdir);
        }

        public string Compile(string src, string name = "luau", int offset = 0)
        {
            string fn = rand.Next(100000, 999999).ToString() + ".bin";
            File.WriteAllText(tmpdir + "\\" + fn, src);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = bindir + "\\compiler.exe",
                    Arguments = "./tmp/" + fn + " " + name + " " + offset.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string data = "";
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                data += line;
            }
            File.Delete(tmpdir + "\\" + fn);
            return data;
        }
    }
}
