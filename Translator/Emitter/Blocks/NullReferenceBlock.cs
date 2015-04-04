using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class NullReferenceBlock : AbstractEmitterBlock
    {
        public NullReferenceBlock(IEmitter emitter, AstNode nullNode)
        {
            this.Emitter = emitter;
            this.NullNode = nullNode;
        }

        public AstNode NullNode 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            this.Write("null");
        }
    }
}