using Bridge.Contract;
using Microsoft.Ajax.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bridge.Translator
{
    public partial class Translator : ITranslator
    {
        public const string Bridge_ASSEMBLY = "Bridge";
        
        public Translator(string location)
        {
            this.Location = location;
            this.Validator = this.CreateValidator();
        }

        public Dictionary<string, string> Translate()
        {            
            var config = this.ReadConfig();
            this.Plugins = Bridge.Translator.Plugins.GetPlugins(this, config);
            this.Plugins.OnConfigRead(config);

            if (config != null && !string.IsNullOrWhiteSpace(config.BeforeBuild))
            {
                this.RunEvent(config.BeforeBuild);
            }
            
            this.ReadProjectFile();

            if (this.Rebuild || !File.Exists(this.AssemblyLocation))
            {
                this.BuildAssembly();
            }

            var references = this.InspectReferences();            

            var resolver = new MemberResolver(this.SourceFiles, Emitter.ToAssemblyReferences(references));

            this.InspectTypes(resolver, config);
            
            resolver.CanFreeze = true;
            var emitter = this.CreateEmitter(resolver);            
            emitter.AssemblyInfo = this.AssemblyInfo;            
            emitter.ChangeCase = this.ChangeCase;
            emitter.References = references;
            emitter.SourceFiles = this.SourceFiles;
            emitter.Log = this.Log;
            emitter.Plugins = this.Plugins;
            this.Plugins.BeforeEmit(emitter, this);
            this.Outputs = emitter.Emit();

            return this.Outputs;
        }        

        public virtual string GetCode()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in this.Outputs)
            {
                string code = item.Value;
                builder.AppendLine(code);
            }

            return builder.ToString();
        }

        public virtual void SaveTo(string path, string defaultFileName)
        {
            var minifier = new Minifier();
            foreach (var item in this.Outputs)
            {
                string fileName = item.Key;
                string code = item.Value;

                if (fileName.Contains(Bridge.Translator.AssemblyInfo.DEFAULT_FILENAME))
                {
                    fileName = fileName.Replace(Bridge.Translator.AssemblyInfo.DEFAULT_FILENAME, defaultFileName);
                }

                // Ensure filename contains no ":". It could be used like "c:/absolute/path"
                fileName = fileName.Replace(":", "_");

                // Trim heading slash/backslash off file names until it does not start with slash.
                var oldFNlen = fileName.Length;
                while (Path.IsPathRooted(fileName))
                {
                    fileName = fileName.TrimStart('/', '\\');

                    // Trimming didn't change the path. This way, it will just loop indefinitely.
                    // Also, this means the absolute path specifies a fully-qualified DOS PathName with drive letter.
                    if (fileName.Length == oldFNlen)
                    {
                        break;
                    }
                    oldFNlen = fileName.Length;
                }

                // If 'fileName' is an absolute path, Path.Combine will ignore the 'path' prefix.
                string filePath = Path.Combine(path, fileName);

                var file = new System.IO.FileInfo(filePath);
                file.Directory.Create();
                File.WriteAllText(file.FullName, code, System.Text.UTF8Encoding.UTF8);

                string fn = Path.GetFileNameWithoutExtension(filePath);
                filePath = Path.GetDirectoryName(filePath) + ("\\" + fn + ".min") + Path.GetExtension(filePath);
                file = new System.IO.FileInfo(filePath);
                file.Directory.Create();
                File.WriteAllText(file.FullName, minifier.MinifyJavaScript(code), System.Text.UTF8Encoding.UTF8);
            }            
        }

        protected virtual Emitter CreateEmitter(IMemberResolver resolver)
        {
            return new Emitter(this.TypeDefinitions, this.BridgeTypes, this.Types, this.Validator, resolver, this.TypeInfoDefinitions);
        }

        protected virtual Validator CreateValidator()
        {
            return new Validator();
        }

        public static void ExtractCore(string clrPath, string outputPath)
        {
            Translator.ExtractCore(clrPath, outputPath, false);
        }

        public static void ExtractCore(string clrPath, string outputPath, bool nodebug)
        {
            var assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(clrPath);
            var resourceName = "Bridge.Resources.bridge.js";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    File.WriteAllText(Path.Combine(outputPath, "bridge.js"), reader.ReadToEnd());
                }
            }

            if (!nodebug)
            {
                resourceName = "Bridge.Resources.bridge.min.js";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        File.WriteAllText(Path.Combine(outputPath, "bridge.min.js"), reader.ReadToEnd());
                    }
                }
            }
        }
    }
}
