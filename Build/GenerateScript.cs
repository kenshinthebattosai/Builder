using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Bridge.Build
{
    public class GenerateScript : Task
    {        
        [Required]
        public ITaskItem Assembly
        {
            get;
            set;
        }

        [Required]
        public string OutputPath
        {
            get;
            set;
        }

        [Required]
        public string ProjectPath
        {
            get;
            set;
        }

        [Required]
        public string AssembliesPath
        {
            get;
            set;
        }

        private bool changeCase = true;

        public bool ChangeCase
        {
            get
            {
                return this.changeCase;
            }
            set
            {
                this.changeCase = value;
            }
        }

        public bool NoCore
        {
            get;
            set;
        }

        public string Configuration
        {
            get;
            set;
        }

        protected virtual void LogMessage(string level, string message)
        {
            level = level ?? "message";

            switch(level.ToLowerInvariant())
            {
                case "message":
                    this.Log.LogMessage(message);
                    break;
                case "warning":
                    this.Log.LogWarning(message);
                    break;
                case "error":
                    this.Log.LogError(message);
                    break;
            }
        }

        public override bool Execute()
        {
            var success = true;

            try
            {
                var translator = new Bridge.NET.Translator(this.ProjectPath);
                translator.Configuration = this.Configuration;
                translator.FoundationLocation = Path.Combine(this.AssembliesPath, "Bridge.Foundation.dll");
                translator.Rebuild = false;
                translator.ChangeCase = this.ChangeCase;
                translator.Log = this.LogMessage;
                translator.Translate();

                string fileName = Path.GetFileNameWithoutExtension(this.Assembly.ItemSpec) + ".js";

                string outputDir = !string.IsNullOrWhiteSpace(translator.AssemblyInfo.OutputDir) ?
                                        Path.Combine(Path.GetDirectoryName(this.ProjectPath), translator.AssemblyInfo.OutputDir) :
                                        this.OutputPath;
                
                if (translator.Outputs.Count == 1)
                {
                    translator.SaveToFile(outputDir, fileName);
                }
                else
                {
                    translator.SaveTo(outputDir, fileName);
                }

                if (!this.NoCore)
                {
                    Bridge.NET.Translator.ExtractCore(translator.FoundationLocation, outputDir);
                }

                if (!string.IsNullOrWhiteSpace(translator.AssemblyInfo.AfterBuild))
                {
                    translator.RunEvent(translator.AssemblyInfo.AfterBuild);
                }
            }
            catch (Exception e)
            {
                this.Log.LogError("Error: {0}", e.Message);
                success = false;
            }

            return success;
        }
    }
}
