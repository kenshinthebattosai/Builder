using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class EventDeclarationBlock : AbstractEmitterBlock
    {
        public EventDeclarationBlock(IEmitter emitter, EventDeclaration eventDeclaration)
        {
            this.Emitter = emitter;
            this.EventDeclaration = eventDeclaration;
        }

        public EventDeclaration EventDeclaration 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            var vars = this.EventDeclaration.Variables;
        }
    }
}