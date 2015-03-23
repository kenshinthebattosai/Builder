using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Translator
{
    public class FieldBlock : AbstractEmitterBlock
    {
        public FieldBlock(IEmitter emitter, ITypeInfo typeInfo, bool staticBlock)
        {
            this.Emitter = emitter;
            this.TypeInfo = typeInfo;
            this.StaticBlock = staticBlock;
        }

        public ITypeInfo TypeInfo 
        { 
            get; 
            set; 
        }

        public bool StaticBlock
        {
            get;
            set;
        }

        public override void Emit()
        {
            this.EmitFields(this.StaticBlock ? this.TypeInfo.StaticConfig : this.TypeInfo.InstanceConfig);            
        }

        protected virtual void EmitFields(TypeConfigInfo info)
        {
            if (info.Fields.Count > 0)
            {
                this.WriteObject("fields", info.Fields);
                this.Emitter.Comma = true;
            }

            if (info.Events.Count > 0)
            {
                this.WriteObject("events", info.Events);
                this.Emitter.Comma = true;
            }

            if (info.Properties.Count > 0)
            {
                this.WriteObject("properties", info.Properties);
                this.Emitter.Comma = true;
            }

            if (info.Alias.Count > 0)
            {
                this.WriteAlias("alias", info.Alias);
                this.Emitter.Comma = true;
            }
        }

        protected virtual void WriteObject(string objectName, List<TypeConfigItem> members)
        {
            if (objectName != null)
            {
                this.EnsureComma();
                this.Write(objectName);

                this.WriteColon();
                this.BeginBlock();
            }

            foreach (var member in members)
            {
                this.EnsureComma();
                this.Write(member.GetName(this.Emitter));
                this.WriteColon();

                var primitiveExpr = member.Initializer as PrimitiveExpression;
                if (primitiveExpr != null && primitiveExpr.Value is AstType)
                {
                    this.Write("new " + Helpers.TranslateTypeReference((AstType)primitiveExpr.Value, this.Emitter) + "()");
                }
                else
                {
                    member.Initializer.AcceptVisitor(this.Emitter);
                }
                this.Emitter.Comma = true;
            }

            this.WriteNewLine();
            this.EndBlock();
        }

        protected virtual void WriteAlias(string objectName, List<TypeConfigItem> members)
        {
            /*if (objectName != null)
            {
                this.EnsureComma();
                this.Write(objectName);

                this.WriteColon();
                this.BeginBlock();
            }

            foreach (var member in members)
            {
                this.EnsureComma();
                this.Write(member.Item1);
                this.WriteColon();

                this.Write(member.Item2);
                this.Emitter.Comma = true;
            }

            this.WriteNewLine();
            this.EndBlock();*/
        }       
    }
}
