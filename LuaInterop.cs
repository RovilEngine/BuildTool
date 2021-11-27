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
                    try
                    {
                        file.Delete();
                    }
                    catch { }
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
                    Arguments = "\"./tmp/" + fn + "\" \"" + name + "\" " + offset.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                proc.Start();
            } catch(Exception e)
            {
                Logger.Error(e.ToString());
            }
            string data = "";
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                data += line;
            }
            if (proc.ExitCode != 0)
            {
                return "-- [!] This script failed to compile due to an error in the original script.\nreturn{0x0;};";
            }
            File.Delete(tmpdir + "\\" + fn);
            return data;
        }
    }
}
