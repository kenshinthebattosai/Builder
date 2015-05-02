using System;
using System.Diagnostics;
using Bridge.Contract;
using System.IO;
using Newtonsoft.Json;

namespace Bridge.Translator
{
    public partial class Translator
    {
        private void ConvertConfigPaths(IAssemblyInfo assemblyInfo)
        {
            if (!string.IsNullOrWhiteSpace(assemblyInfo.AfterBuild))
            {
                assemblyInfo.AfterBuild = ConvertPath(assemblyInfo.AfterBuild);
            }

            if (!string.IsNullOrWhiteSpace(assemblyInfo.BeforeBuild))
            {
                assemblyInfo.BeforeBuild = ConvertPath(assemblyInfo.BeforeBuild);
            }

            if (!string.IsNullOrWhiteSpace(assemblyInfo.Output))
            {
                assemblyInfo.Output = ConvertPath(assemblyInfo.Output);
            }

            if (!string.IsNullOrWhiteSpace(assemblyInfo.PluginsPath))
            {
                assemblyInfo.PluginsPath = ConvertPath(assemblyInfo.PluginsPath);
            }
        }

        private string ConvertPath(string path)
        {
            if (Path.DirectorySeparatorChar != '/')
            {
                path = path.Replace('/', Path.DirectorySeparatorChar);
            }

            if (Path.DirectorySeparatorChar != '\\')
            {
                path.Replace('\\', Path.DirectorySeparatorChar);
            }

            return path;
        }

        protected virtual IAssemblyInfo ReadConfig()
        {
            var folder = this.FolderMode ? this.Location : Path.GetDirectoryName(this.Location);
            var path = folder + Path.DirectorySeparatorChar + "bridge" + Path.DirectorySeparatorChar + "bridge.json";

            if (!File.Exists(path))
            {
                path = folder + Path.DirectorySeparatorChar + "bridge.json";
            }

            if (!File.Exists(path))
            {
                path = folder + Path.DirectorySeparatorChar + "Bridge.NET" + Path.DirectorySeparatorChar + "bridge.json";
            }

            if (!File.Exists(path))
            {
                var config = new AssemblyInfo();
                return config;
            }

            try
            {
                var json = File.ReadAllText(path);
                IAssemblyInfo assemblyInfo = JsonConvert.DeserializeObject<AssemblyInfo>(json);

                if (assemblyInfo == null)
                {
                    assemblyInfo = new AssemblyInfo();
                }
                
                // Convert '/' and '\\' to platform-specific path separator.
                ConvertConfigPaths(assemblyInfo);

                return assemblyInfo;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Cannot read bridge.json", e);
            }
        }

        public virtual void RunEvent(string e)
        {
            var info = new ProcessStartInfo()
            {
                FileName = e
            };
            info.WindowStyle = ProcessWindowStyle.Hidden;

            if (!File.Exists(e))
            {
                throw new Exception("The specified file '" + e + "' couldn't be found." +
                    "\nWarning: Bridge.NET translator working directory: " + Directory.GetCurrentDirectory());
            }

            using (var p = Process.Start(info))
            {
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    throw new Exception("Error: The command '" + e + "' returned with exit code: " + p.ExitCode);
                }
            }
        }
    }
}
