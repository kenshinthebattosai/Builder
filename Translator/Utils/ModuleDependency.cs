using Bridge.Contract;
namespace Bridge.Translator
{
    public class ModuleDependency : IPluginDependency
    {
        public string DependencyName
        {
            get;
            set;
        }

        public string VariableName
        {
            get;
            set;
        }
    }
}
