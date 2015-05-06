using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bridge.Contract
{
    public interface IPlugin
    {
        IEnumerable<string> GetConstructorInjectors(IConstructorBlock constructorBlock);
        bool HasConstructorInjectors(IConstructorBlock constructorBlock);
        void OnConfigRead(IAssemblyInfo config);
        void BeforeEmit(IEmitter emitter, ITranslator translator);
        void AfterEmit(IEmitter emitter, ITranslator translator);
        void BeforeTypesEmit(IEmitter emitter, IList<ITypeInfo> types);
        void AfterTypesEmit(IEmitter emitter, IList<ITypeInfo> types);
        void BeforeTypeEmit(IEmitter emitter, ITypeInfo type);
        void AfterTypeEmit(IEmitter emitter, ITypeInfo type);
    }
}
