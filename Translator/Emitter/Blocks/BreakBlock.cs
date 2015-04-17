using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class BreakBlock : AbstractEmitterBlock
    {
        public BreakBlock(IEmitter emitter, BreakStatement breakStatement)
        {
            this.Emitter = emitter;
            this.BreakStatement = breakStatement;
        }

        public BreakStatement BreakStatement 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            if (this.Emitter.JumpStatements != null)
            {
                var finallyNode = this.GetParentFinallyBlock(this.BreakStatement, true);

                if (finallyNode != null)
                {
                    var hashcode = finallyNode.GetHashCode();
                    this.Emitter.AsyncBlock.JumpLabels.Add(new AsyncJumpLabel { Node = finallyNode, Output = this.Emitter.Output });
                    this.Write("$step = ${" + hashcode + "};");
                    this.WriteNewLine();
                    this.Write("$jumpFromFinally = ");
                    this.Emitter.JumpStatements.Add(new JumpInfo(this.Emitter.Output, this.Emitter.Output.Length, true));
                    this.WriteSemiColon();
                    this.WriteNewLine();
                }
                else
                {
                    this.Write("$step = ");
                    this.Emitter.JumpStatements.Add(new JumpInfo(this.Emitter.Output, this.Emitter.Output.Length, true));

                    this.WriteSemiColon();
                    this.WriteNewLine();
                }

                this.Write("continue");
            }
            else
            {
                this.Write("break");
            }
            
            this.WriteSemiColon();
            this.WriteNewLine();
        }
    }
}
