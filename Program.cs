using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace RobloxScriptCompiler
{
    static class Program
    {
        static Stopwatch elapsed;
        static LuaInterop Lua;
        static string ver = typeof(Program).Assembly.GetName().Version.ToString();
        static Dictionary<string, string> assets = new Dictionary<string, string> { };
        static string base_dir = Path.Combine(Directory.GetCurrentDirectory(), "bin");
        static Random random = new Random();
        static string[] args;
        static string build_id = Guid.NewGuid().ToString().Replace("-", "");

        [STAThread]
        static void Main(string[] argv)
        {
            args = argv;
            assets.Add("compiler", "exe");
            assets.Add("liblua", "dll");
            assets.Add("client", "xml");
            elapsed = new Stopwatch();
            Console.Title = "Rovil Build Tool v" + ver;
            Logger.Info("Welcome to the build tool");
            Logger.Info("Checking for updates");
            CheckForUpdates();
            Lua = new LuaInterop();
            Logger.Info("Please make sure you have exported your place as a \".rbxlx\" (Roblox XML) file");
            Logger.Info("Open the place file you would like to build");
            PromptToCompile(args.Length >= 1 ? args[0] : null);
            Console.Write("Press any key to exit.");
            Console.ReadKey();
        }

        static void CheckForUpdates()
        {
            Directory.CreateDirectory(base_dir);
            string meta_fn = Path.Combine(base_dir, "metadata.json");
            if (!File.Exists(meta_fn)) File.WriteAllText(meta_fn, "{}");
            var metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(meta_fn));
            using (WebClient wc = new WebClient())
            {
                string data = wc.DownloadString("https://db.afo.workers.dev/rovil_resources.json");
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                foreach (var asset in assets)
                {
                    if (json.ContainsKey(asset.Key))
                    {
                        string file = json[asset.Key];
                        bool isLatestVersion = metadata.ContainsKey(asset.Key) && metadata[asset.Key] == file;
                        if (!isLatestVersion)
                        {
                            string fn = asset.Key + "." + asset.Value;
                            Logger.Info("Downloading newest version of " + fn);
                            wc.DownloadFile(file, Path.Combine(base_dir, fn));
                            Logger.Ok("Done");
                            metadata[asset.Key] = file;
                        }
                    } else
                    {
                        Logger.Error(asset.Key.ToString() + " could not be downloaded");
                        return;
                    }
                }
            }
            File.WriteAllText(meta_fn, JsonConvert.SerializeObject(metadata));
        }

        enum ScriptType
        {
            LocalScript,
            ModuleScript
        }
        static void ParseScripts(XmlNodeList scripts, int offset, ScriptType type = ScriptType.LocalScript)
        {
            bool isModule = type == ScriptType.ModuleScript;
            int count = 0;
            foreach (XmlNode script in scripts)
            {
                count++;
                XmlNode properties = script.SelectSingleNode("./Properties[1]");
                XmlNode name = properties.SelectSingleNode("./string[@name='Name'][1]");
                XmlNode source = properties.SelectSingleNode("./ProtectedString[@name='Source'][1]");
                XmlNode guid = properties.SelectSingleNode("./string[@name='ScriptGuid'][1]");
                XmlNode referent = script.Attributes.GetNamedItem("referent");
                if (name == null || source == null || guid == null || referent == null)
                {
                    Logger.Debug("Script attribute was null or ignored!");
                    continue;
                }
                Logger.Info(count.ToString() + "/" + scripts.Count.ToString() + " - Loading \"" + name.InnerText.ToString() + "\"");
                if (referent != null
                && source != null
                && guid != null
                && !name.InnerText.StartsWith("#")
                && !name.InnerText.StartsWith("*")
                && !name.InnerText.StartsWith("!")
                && !name.InnerText.StartsWith("@"))
                {
                    Logger.Debug("Compiling \"" + name.InnerText.ToString() + "\" (source length " + source.InnerText.Length.ToString() + ")");
                    script.Attributes.GetNamedItem("class").Value = "ModuleScript";
                    source.InnerText = Lua.Compile(source.InnerText, name.InnerText, offset);
                    script.Attributes.SetNamedItem(referent);
                    name.InnerText = (isModule ? "*" : "#") + name.InnerText.ToString();
                    Logger.Ok("Source compiled successfully");
                }
                else
                {
                    Logger.Warn("Skipping script");
                }
                ParseScripts(script.SelectNodes("./Item[@class='" + (isModule ? "ModuleScript" : "LocalScript") + "']"), offset, type);
            }
        }

        static void SetClientMetadata(XmlDocument doc, Dictionary<string, object> metadata)
        {
            XmlNode buildData = doc.SelectSingleNode("//Item[@class='StringValue'][1]");
            XmlNode properties = buildData.SelectSingleNode("./Properties[1]");
            XmlNode value = properties.SelectSingleNode("./string[@name='Value'][1]");
            value.InnerText = JsonConvert.SerializeObject(metadata);
        }

        static void CompileFile(string fileName, int offset)
        {
            var build_options = new Dictionary<string, object> { };
            build_options.Add("BuildId", build_id);
            build_options.Add("Version", ver + "b");
            build_options.Add("Offset", offset);
            //build_options.Add("Arguments", args);
            elapsed.Start();
            Logger.Info("Loading place");
            XmlDocument doc = new XmlDocument();
            string infile = fileName;
            string outfile = Path.GetDirectoryName(infile) + "\\Compiled_" + Path.GetFileName(infile);
            doc.Load(infile);
            Logger.Info("Loading ModuleScripts");
            ParseScripts(doc.DocumentElement.SelectNodes("//Item[@class='ModuleScript']"), offset, ScriptType.ModuleScript);
            Logger.Ok("All ModuleScripts compiled");
            Logger.Info("Loading LocalScripts");
            ParseScripts(doc.DocumentElement.SelectNodes("//Item[@class='LocalScript']"), offset, ScriptType.LocalScript);
            Logger.Ok("All LocalScripts compiled");
            doc.Save(outfile);
            Logger.Info("Adding client script");
            string client_data = File.ReadAllText(Path.Combine(base_dir, "client.xml"));
            XmlDocument client_xml = new XmlDocument();
            client_xml.LoadXml(client_data);
            SetClientMetadata(client_xml, build_options);
            XmlNode replicated_first = doc.DocumentElement.SelectSingleNode("//Item[@class='ReplicatedFirst']");
            var emptyNamepsaces = new XmlSerializerNamespaces(new[] {
                XmlQualifiedName.Empty
            });
            using (var writer = replicated_first.CreateNavigator().AppendChild())
            {
                var serializer = new XmlSerializer(client_xml.GetType());
                writer.WriteWhitespace("");
                serializer.Serialize(writer, client_xml, emptyNamepsaces);
                writer.Close();
            }
            doc.Save(outfile);
            elapsed.Stop();
            Logger.Ok("Done (took " + (elapsed.ElapsedMilliseconds / 1000).ToString() + "s)");
            if (Logger.Debug("Opening studio"))
            {
                Process studio = new Process();
                studio.StartInfo = new ProcessStartInfo()
                {
                    FileName = outfile
                };
                studio.Start();
                studio.WaitForExit();
            } else if (args.Length == 0)
            {
                Process exporer = new Process();
                exporer.StartInfo = new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = "/select," + outfile
                };
                exporer.Start();
            }
        }

        static void PromptToCompile(string openfile = null)
        {
            var offset = 0;//random.Next(26, 255);
            Logger.Debug("Using an offset of " + offset.ToString());
            Logger.Debug("Build id " + build_id);
            if (openfile == null)
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Roblox XML Place Files (*.rbxlx)|*.rbxlx";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        CompileFile(openFileDialog.FileName, offset);
                    }
                    else PromptToCompile();
                }
            }
            else CompileFile(openfile, offset);
        }
    }
}
