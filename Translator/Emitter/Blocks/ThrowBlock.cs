using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class ThrowBlock : AbstractEmitterBlock
    {
        public ThrowBlock(IEmitter emitter, ThrowStatement throwStatement) : base(emitter, throwStatement)
        {
            this.Emitter = emitter;
            this.ThrowStatement = throwStatement;
        }

        public ThrowStatement ThrowStatement 
        { 
            get; 
            set; 
        }

        protected override void DoEmit()
        {
            var oldValue = this.Emitter.ReplaceAwaiterByVar;

            if (this.Emitter.IsAsync)
            {                
                this.WriteAwaiters(this.ThrowStatement.Expression);
                this.Emitter.ReplaceAwaiterByVar = true;
            }
            
            this.WriteThrow();            
            this.ThrowStatement.Expression.AcceptVisitor(this.Emitter);
            this.WriteSemiColon();
            this.WriteNewLine();
            this.Emitter.ReplaceAwaiterByVar = oldValue;
        }
    }
}