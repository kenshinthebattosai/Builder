using Bridge.Contract;
using Mono.Cecil;
using Object.Net.Utilities;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bridge.Translator
{
    public class ClassBlock : AbstractEmitterBlock
    {
        public ClassBlock(IEmitter emitter, ITypeInfo typeInfo)
        {
            this.Emitter = emitter;
            this.TypeInfo = typeInfo;
        }

        public ITypeInfo TypeInfo
        {
            get;
            set;
        }

        public bool IsGeneric
        {
            get;
            set;
        }

        public int StartPosition
        {
            get;
            set;
        }

        public override void Emit()
        {
            this.EmitClassHeader();
            this.EmitStaticBlock();
            this.EmitInstantiableBlock();
            this.EmitClassEnd();    
        }        

        protected virtual void EmitClassHeader()
        {
            TypeDefinition baseType = this.Emitter.GetBaseTypeDefinition();
            var typeDef = this.Emitter.TypeDefinitions[this.TypeInfo.GenericFullName];
            string name = this.Emitter.Validator.GetCustomTypeName(typeDef);
            this.IsGeneric = typeDef.GenericParameters.Count > 0;

            if (name.IsEmpty())
            {
                name = this.TypeInfo.GenericFullName;
            }

            if (this.IsGeneric)
            {
                this.WriteGenericHeader(name, typeDef);
            }

            this.Write(Bridge.Translator.Emitter.ROOT + ".define");

            this.WriteOpenParentheses();

            if (this.IsGeneric)
            {
                this.Write("$$name");
                this.StartPosition = this.Emitter.Output.Length;
                this.Write(", ");
            }
            else
            {
                this.Write("'" + name, "'");
                this.StartPosition = this.Emitter.Output.Length;
                this.Write(", ");
            }            
            
            this.BeginBlock();

            string extend = this.Emitter.GetTypeHierarchy();

            if (extend.IsNotEmpty() && !this.TypeInfo.IsEnum)
            {
                if (this.TypeInfo.InstanceMethods.Any(m => m.Value.Any(subm => this.Emitter.GetEntityName(subm) == "inherits")))
                {
                    this.Write("$");
                }

                this.Write("inherits");
                this.WriteColon();
                this.Write(extend);
                this.Emitter.Comma = true;
            }

            if (this.TypeInfo.Module != null)
            {
                this.WriteScope();
            }

            if (typeDef.GenericParameters.Count > 0)
            {
                this.EnsureComma();
            }
        }

        protected virtual void WriteScope()
        {
            this.EnsureComma();
            this.Write("$scope");
            this.WriteColon();
            this.Write("exports");
            this.Emitter.Comma = true;
        }

        protected virtual void WriteGenericHeader(string name, TypeDefinition typeDef)
        {
            this.Write("Bridge.Class.generic");
            this.WriteOpenParentheses();
            this.Write("'" + name, "'");
            this.Emitter.Comma = true;

            if (this.TypeInfo.Module != null)
            {
                this.WriteScope();
            }

            this.EnsureComma(false);

            this.WriteFunction();
            this.WriteOpenParentheses();

            foreach (var p in typeDef.GenericParameters)
            {
                this.EnsureComma(false);
                this.Write(p.Name);
                this.Emitter.Comma = true;
            }

            this.Emitter.Comma = false;

            this.WriteCloseParentheses();
            this.WriteSpace();
            this.BeginBlock();
            this.WriteVar(true);
            this.Write("$$name = Bridge.Class.genericName");
            this.WriteOpenParentheses();
            this.Write("'" + name, "'");
            this.Emitter.Comma = true;

            foreach (var p in typeDef.GenericParameters)
            {
                this.EnsureComma(false);
                this.Write(p.Name);
                this.Emitter.Comma = true;
            }

            this.Emitter.Comma = false;
            this.WriteCloseParentheses();
            this.WriteSemiColon(true);

            this.Write("return Bridge.Class.cache[$$name] || (Bridge.Class.cache[$$name] = ");
        }        

        protected virtual void EmitStaticBlock()
        {
            if (this.TypeInfo.HasStatic)
            {
                this.EnsureComma();

                if (this.TypeInfo.InstanceMethods.Any(m => m.Value.Any(subm => this.Emitter.GetEntityName(subm) == "statics")))
                {
                    this.Write("$");
                }

                this.Write("statics");
                this.WriteColon();
                this.BeginBlock();

                new ConstructorBlock(this.Emitter, this.TypeInfo, true).Emit();                
                new MethodBlock(this.Emitter, this.TypeInfo, true).Emit();

                this.WriteNewLine();
                this.EndBlock();
                this.Emitter.Comma = true;
            }
        }

        protected virtual void EmitInstantiableBlock()
        {
            var ctorBlock = new ConstructorBlock(this.Emitter, this.TypeInfo, false);

            if (this.TypeInfo.HasInstantiable || this.Emitter.Plugins.HasConstructorInjectors(ctorBlock))
            {
                this.EnsureComma();
                ctorBlock.Emit();
                new MethodBlock(this.Emitter, this.TypeInfo, false).Emit();
            }
            else
            {
                this.Emitter.Comma = false;
            }
        }

        protected virtual void EmitClassEnd()
        {
            this.WriteNewLine();
            this.EndBlock();

            var classStr = this.Emitter.Output.ToString().Substring(this.StartPosition);

            if(Regex.IsMatch(classStr, ",\\s*\\{\\s*\\}", RegexOptions.Multiline))
            {
                this.Emitter.Output.Remove(this.StartPosition, this.Emitter.Output.Length - this.StartPosition);
            }

            this.WriteCloseParentheses();

            if (this.IsGeneric)
            {
                this.WriteCloseParentheses();
                this.WriteSemiColon();
                this.WriteNewLine();
            }
            else
            {
                this.WriteSemiColon();
                this.WriteNewLine();
                this.WriteNewLine();
            }
            

            if (this.IsGeneric)
            {
                this.WriteGenericEnd();
            }
        }

        protected virtual void WriteGenericEnd()
        {
            this.EndBlock();
            this.WriteCloseParentheses();
            this.WriteSemiColon();
            this.WriteNewLine();
            this.WriteNewLine();
        }
    }
}
