using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;

namespace Bridge.Translator
{
    public class TypeBlock : AbstractEmitterBlock
    {
        public TypeBlock(IEmitter emitter, AstType type)
        {
            this.Emitter = emitter;
            this.Type = type;
        }

        public AstType Type 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            this.EmitTypeReference();
        }

        protected virtual void EmitTypeReference()
        {
            AstType astType = this.Type;
            this.Write(BridgeTypes.ToJsName(astType, this.Emitter));
        }
    }
}