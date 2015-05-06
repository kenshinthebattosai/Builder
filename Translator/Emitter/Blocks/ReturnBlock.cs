using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;

namespace Bridge.Translator
{
    public class ReturnBlock : AbstractEmitterBlock
    {
        public ReturnBlock(IEmitter emitter, ReturnStatement returnStatement)
            : base(emitter, returnStatement)
        {
            this.Emitter = emitter;
            this.ReturnStatement = returnStatement;
        }

        public ReturnStatement ReturnStatement
        {
            get;
            set;
        }

        protected override void DoEmit()
        {
            this.VisitReturnStatement();
        }

        protected void VisitReturnStatement()
        {
            ReturnStatement returnStatement = this.ReturnStatement;

            if (this.Emitter.IsAsync)
            {
                var finallyNode = this.GetParentFinallyBlock(returnStatement, false);

                if (this.Emitter.AsyncBlock != null && this.Emitter.AsyncBlock.IsTaskReturn)
                {
                    this.WriteAwaiters(returnStatement.Expression);

                    if (finallyNode != null)
                    {
                        this.Write("$returnValue = ");
                    }
                    else
                    {
                        this.Write("$returnTask.setResult(");
                    }

                    if (!returnStatement.Expression.IsNull)
                    {
                        var oldValue = this.Emitter.ReplaceAwaiterByVar;
                        this.Emitter.ReplaceAwaiterByVar = true;
                        returnStatement.Expression.AcceptVisitor(this.Emitter);
                        this.Emitter.ReplaceAwaiterByVar = oldValue;
                    }
                    else
                    {
                        this.Write("null");
                    }

                    if (finallyNode != null)
                    {
                        this.Write(";");
                    }
                    else
                    {
                        this.Write(");");
                    }


                    this.WriteNewLine();
                }

                if (finallyNode != null)
                {
                    var hashcode = finallyNode.GetHashCode();
                    this.Emitter.AsyncBlock.JumpLabels.Add(new AsyncJumpLabel { Node = finallyNode, Output = this.Emitter.Output });
                    this.Write("$step = ${" + hashcode + "};");
                    this.WriteNewLine();
                    this.Write("continue;");
                }
                else
                {
                    this.WriteReturn(false);
                    this.WriteSemiColon();
                    this.WriteNewLine();
                }
            }
            else
            {
                this.WriteReturn(false);

                if (!returnStatement.Expression.IsNull)
                {
                    this.WriteSpace();
                    returnStatement.Expression.AcceptVisitor(this.Emitter);
                }

                this.WriteSemiColon();
                this.WriteNewLine();
            }
        }
    }
}