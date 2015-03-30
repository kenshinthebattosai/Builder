using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;
using Object.Net.Utilities;
using Bridge.Contract;

namespace Bridge.Translator
{
    public class EmitBlock : AbstractEmitterBlock
    {
        public EmitBlock(IEmitter emitter)
        {
            this.Emitter = emitter;
        }

        private void InitEmitter()
        {
            this.Emitter.Output = new StringBuilder();
            this.Emitter.Locals = null;
            this.Emitter.LocalsStack = null;
            this.Emitter.IteratorCount = 0;
            this.Emitter.ThisRefCounter = 0;
            this.Emitter.Writers = new Stack<Tuple<string, StringBuilder, bool>>();
            this.Emitter.IsAssignment = false;
            this.Emitter.Level = 0;
            this.Emitter.IsNewLine = true;
            this.Emitter.EnableSemicolon = true;
            this.Emitter.Comma = false;
            this.Emitter.CurrentDependencies = new List<IPluginDependency>();
        }

        protected virtual StringBuilder GetOutputForType(ITypeInfo typeInfo)
        {
            string module = null;
            if (typeInfo.Module != null)
            {
                module = typeInfo.Module;
            }
            else if (this.Emitter.AssemblyInfo.Module != null)
            {
                module = this.Emitter.AssemblyInfo.Module;
            }

            var fileName = typeInfo.FileName;

            if (fileName.IsEmpty() && this.Emitter.AssemblyInfo.OutputBy != OutputBy.Project)
            {
                switch (this.Emitter.AssemblyInfo.OutputBy)
                {
                    case OutputBy.ClassPath:
                        fileName = typeInfo.FullName;
                        break;
                    case OutputBy.Class:
                        fileName = typeInfo.Name;
                        break;
                    case OutputBy.Module:
                        fileName = module;
                        break;
                    case OutputBy.NamespacePath:
                    case OutputBy.Namespace:
                        fileName = typeInfo.Namespace;
                        break;
                    default:
                        break;
                }

                var isPathRelated = this.Emitter.AssemblyInfo.OutputBy == OutputBy.ClassPath ||
                                    this.Emitter.AssemblyInfo.OutputBy == OutputBy.NamespacePath;
                if (fileName.IsNotEmpty() && isPathRelated)
                {
                    fileName = fileName.Replace('.', System.IO.Path.DirectorySeparatorChar);
                    if (this.Emitter.AssemblyInfo.StartIndexInName > 0)
                    {
                        fileName = fileName.Substring(this.Emitter.AssemblyInfo.StartIndexInName);
                    }
                }
            }

            if (fileName.IsEmpty() && this.Emitter.AssemblyInfo.FileName != null)
            {
                fileName = this.Emitter.AssemblyInfo.FileName;
            }


            if (fileName.IsEmpty())
            {
                fileName = AssemblyInfo.DEFAULT_FILENAME;
            }

            // Append '.js' extension to file name at translator.Outputs level: this aids in code grouping on files
            // when filesystem is not case sensitive.
            if (!fileName.ToLower().EndsWith("." + Bridge.Translator.AssemblyInfo.JAVASCRIPT_EXTENSION))
            {
                fileName += "." + Bridge.Translator.AssemblyInfo.JAVASCRIPT_EXTENSION;
            }

            if (this.Emitter.AssemblyInfo.FileNameCasing == FileNameCaseConvert.CamelCase)
            {
                // Turns the fileName into 'lowerCamelCasing'. Considering dots to separate words
                // e.g.: NameSpace.PrivateSTAAgency => nameSpace.privateSTAAgency
                var extensionLength = Bridge.Translator.AssemblyInfo.JAVASCRIPT_EXTENSION.Length + 1;
                var fileNameLastPos = fileName.Length - extensionLength;
                var camelCasedString = "";

                // State machine-like loop to iterate thru filename converting case where it applies.
                char currChar, currLCChar;
                var wordStart = true; // on the beginning
                for (int i = 0; i < fileNameLastPos; i++)
                {
                    currChar = fileName[i];
                    currLCChar = Char.ToLower(currChar);
                    if (wordStart)
                    {
                        currChar = currLCChar;
                        wordStart = false;
                    }

                    // FIXME: no localization support: á, ç, and other chars will delimit words!
                    if (currLCChar < 'a' || currLCChar > 'z')
                    {
                        wordStart = true; // at every dot occurrence
                    }
                    camelCasedString += currChar;
                }

                // Bind cased name adding back file extension.
                fileName = camelCasedString + fileName.Substring(fileNameLastPos);
            }

            switch (this.Emitter.AssemblyInfo.FileNameCasing)
            {
                case FileNameCaseConvert.Lowercase:
                    fileName = fileName.ToLower();
                    break;
                default:
                    var lcFileName = fileName.ToLower();

                    // Find a file name that matches (case-insensitive) and use it as file name (if found)
                    // The used file name will use the same casing of the existing one.
                    foreach (var existingFile in this.Emitter.Outputs.Keys)
                    {
                        if (lcFileName == existingFile.ToLower())
                        {
                            fileName = existingFile;
                        }
                    }
                    break;
            }

            IEmitterOutput output = null;

            if (this.Emitter.Outputs.ContainsKey(fileName))
            {
                output = this.Emitter.Outputs[fileName];
            }
            else
            {
                output = new EmitterOutput(fileName);
                this.Emitter.Outputs.Add(fileName, output);
            }

            if (module == null)
            {
                return output.NonModuletOutput;
            }

            if (module == "")
            {
                module = Bridge.Translator.AssemblyInfo.DEFAULT_FILENAME;
            }

            if (output.ModuleOutput.ContainsKey(module))
            {
                this.Emitter.CurrentDependencies = output.ModuleDependencies[module];
                return output.ModuleOutput[module];
            }

            StringBuilder moduleOutput = new StringBuilder();
            output.ModuleOutput.Add(module, moduleOutput);
            var dependencies = new List<IPluginDependency>();
            output.ModuleDependencies.Add(module, dependencies);

            if (typeInfo.Dependencies.Count > 0)
            {
                dependencies.AddRange(typeInfo.Dependencies);
            }

            this.Emitter.CurrentDependencies = dependencies;
            return moduleOutput;
        }

        public override void Emit()
        {
            this.Emitter.Writers = new Stack<Tuple<string, StringBuilder, bool>>();
            this.Emitter.Outputs = new EmitterOutputs();

            foreach (var type in this.Emitter.Types)
            {
                this.InitEmitter();

                ITypeInfo typeInfo;
                if (this.Emitter.TypeInfoDefinitions.ContainsKey(type.GenericFullName))
                {
                    typeInfo = this.Emitter.TypeInfoDefinitions[type.GenericFullName];

                    type.Module = typeInfo.Module;
                    type.FileName = typeInfo.FileName;
                    type.Dependencies = type.Dependencies;
                    typeInfo = type;
                }
                else
                {
                    typeInfo = type;
                }

                this.Emitter.Output = this.GetOutputForType(typeInfo);
                this.Emitter.TypeInfo = type;

                if (this.Emitter.TypeInfo.Module != null)
                {
                    this.Indent();
                }

                new ClassBlock(this.Emitter, this.Emitter.TypeInfo).Emit();
            }
            this.RemovePenultimateEmptyLines(true);
        }
    }
}
