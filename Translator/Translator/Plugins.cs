using Bridge.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bridge.Translator
{
    public class Plugins : IPlugins
    {
        public static IPlugins GetPlugins(ITranslator translator, IAssemblyInfo config)
        {
            string path = null;
            if (!string.IsNullOrWhiteSpace(config.PluginsPath))
            {
                path = Path.Combine(translator.FolderMode ? translator.Location : Path.GetDirectoryName(translator.Location), config.PluginsPath);
            }
            else
            {
                path = Path.Combine(translator.FolderMode ? translator.Location : Path.GetDirectoryName(translator.Location), "Bridge" + Path.DirectorySeparatorChar + "plugins");
            }
            
            if (!System.IO.Directory.Exists(path)) 
            {
                return new Plugins() { plugins = new IPlugin[0] };
            }

            DirectoryCatalog dirCatalog = new DirectoryCatalog(path, "*.dll");            
            var catalog = new AggregateCatalog(dirCatalog);

            CompositionContainer container = new CompositionContainer(catalog);
            var plugins = new Plugins();
            container.ComposeParts(plugins);

            return plugins;
        }

        [ImportMany]
        private IEnumerable<IPlugin> plugins;

        public IEnumerable<IPlugin> Parts
        {
            get
            {
                return this.plugins;
            }
        }

        public void OnConfigRead(IAssemblyInfo config)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.OnConfigRead(config);
            }
        }        

        public IEnumerable<string> GetConstructorInjectors(IConstructorBlock constructorBlock)
        {
            IEnumerable<string> result = new List<string>();
            foreach (var plugin in this.Parts)
            {
                result = result.Concat(plugin.GetConstructorInjectors(constructorBlock));
            }

            return result;
        }

        public bool HasConstructorInjectors(IConstructorBlock constructorBlock)
        {
            foreach (var plugin in this.Parts)
            {
                if (plugin.HasConstructorInjectors(constructorBlock))
                {
                    return true;
                }
            }

            return false;
        }

        public void BeforeEmit(IEmitter emitter, ITranslator translator)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.BeforeEmit(emitter, translator);
            }
        }

        public void AfterEmit(IEmitter emitter, ITranslator translator)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.AfterEmit(emitter, translator);
            }
        }

        public void BeforeTypesEmit(IEmitter emitter, IList<ITypeInfo> types)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.BeforeTypesEmit(emitter, types);
            }
        }

        public void AfterTypesEmit(IEmitter emitter, IList<ITypeInfo> types)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.AfterTypesEmit(emitter, types);
            }
        }

        public void BeforeTypeEmit(IEmitter emitter, ITypeInfo type)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.BeforeTypeEmit(emitter, type);
            }
        }

        public void AfterTypeEmit(IEmitter emitter, ITypeInfo type)
        {
            foreach (var plugin in this.Parts)
            {
                plugin.AfterTypeEmit(emitter, type);
            }
        }
    }
}
