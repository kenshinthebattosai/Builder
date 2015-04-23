using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Bridge.Translator
{
    public partial class Translator
    {
        protected virtual void ReadProjectFile()
        {
            var doc = XDocument.Load(Location, LoadOptions.SetLineInfo);

            ValidateProject(doc);

            this.BuildAssemblyLocation(doc);
            this.SourceFiles = this.GetSourceFiles(doc);
            this.ParsedSourceFiles = new List<ParsedSourceFile>();
        }

        /// <summary>
        /// Validates project and namespace names against conflicts with Bridge.NET namespaces.
        /// </summary>
        /// <param name="doc">XDocument reference of the .csproj file.</param>
        private void ValidateProject(XDocument doc)
        {
            var valid = true;
            var failList = new HashSet<string>();
            var failNodeList = new List<XElement>();
            var combined_tags = from x in doc.Descendants()
                                where x.Name.LocalName == "RootNamespace" || x.Name.LocalName == "AssemblyName"
                                select x;

            foreach (var tag in combined_tags)
            {
                if (tag.Value == "Bridge" || tag.Value.StartsWith("Bridge."))
                {
                    valid = false;
                    if (!failList.Contains(tag.Value))
                    {
                        failList.Add(tag.Value);
                        failNodeList.Add(tag);
                    }
                }
            }

            if (!valid)
            {
                var offendingSettings = "";
                foreach (var tag in failNodeList)
                {
                    offendingSettings += "Line " + ((IXmlLineInfo)tag).LineNumber + ": <" + tag.Name.LocalName + ">" +
                        tag.Value + "</" + tag.Name.LocalName + ">\n";
                }

                throw new Bridge.Translator.Exception("'Bridge' or 'Bridge.*' names are reserved and may not " +
                    "be used as project names, root namespaces or namespaces.\n" +
                    "Please verify your project settings and rename where it applies.\n" +
                    "Project file: " + this.Location + "\n" +
                    "Offending settings:\n" + offendingSettings
                );
            }
        }

        protected virtual void BuildAssemblyLocation(XDocument doc)
        {
            if (this.AssemblyLocation == null || this.AssemblyLocation.Length == 0)
            {
                this.Configuration = this.Configuration ?? "Debug";
                var outputPath = this.GetOutputPath(doc, this.Configuration);

                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = this.GetOutputPath(doc, "Release");
                }

                this.AssemblyLocation = Path.Combine(outputPath, this.GetAssemblyName(doc) + ".dll");

                if (!File.Exists(this.AssemblyLocation))
                {
                    outputPath = this.GetOutputPath(doc, "Release");
                    this.AssemblyLocation = Path.Combine(outputPath, this.GetAssemblyName(doc) + ".dll");
                }
            }
        }

        protected virtual string GetOutputPath(XDocument doc, string configuration)
        {
            var opnodes = from n in doc.Descendants() where n.Name.LocalName == "OutputPath" select n;
            var nodes = from n in doc.Descendants()
                        where n.Name.LocalName == "OutputPath" &&
                              n.Parent.Attribute("Condition").Value.Contains(configuration)
                        select n;

            if (nodes.Count() != 1)
            {
                Bridge.Translator.Exception.Throw("Unable to determine output path");
            }

            var path = nodes.First().Value;

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Location), path));
            }

            return path;
        }

        protected virtual IList<string> GetSourceFiles(XDocument doc)
        {
            var result = new List<string>();

            var nodeList = from n in doc.Descendants()
                           where n.Name.LocalName == "Compile" &&
                                 !string.IsNullOrWhiteSpace(n.Attribute("Include").Value)
                           select n;

            foreach (var node in nodeList)
            {
                result.Add(node.Attribute("Include").Value);
            }

            return result;
        }

        protected virtual string GetAssemblyName(XDocument doc)
        {
            var nodes = from n in doc.Descendants()
                        where n.Name.LocalName == "AssemblyName"
                        select n;

            if (nodes.Count() != 1)
            {
                Bridge.Translator.Exception.Throw("Unable to determine assembly name");
            }

            return nodes.First().Value;
        }
    }
}
