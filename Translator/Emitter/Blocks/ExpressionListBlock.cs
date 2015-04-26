using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;

namespace Bridge.Translator
{
    public class ExpressionListBlock : AbstractEmitterBlock
    {
        public ExpressionListBlock(IEmitter emitter, IEnumerable<Expression> expressions, Expression paramArg)
            : base(emitter, null)
        {
            this.Emitter = emitter;
            this.Expressions = expressions;
            this.ParamExpression = paramArg;
        }

        public IEnumerable<Expression> Expressions
        {
            get;
            set;
        }

        public Expression ParamExpression
        {
            get;
            set;
        }

        protected override void DoEmit()
        {
            this.EmitExpressionList(this.Expressions, this.ParamExpression);
        }

        protected virtual void EmitExpressionList(IEnumerable<Expression> expressions, Expression paramArg)
        {
            bool needComma = false;
            int count = this.Emitter.Writers.Count;

            foreach (var expr in expressions)
            {
                this.Emitter.Translator.EmitNode = expr;
                if (needComma)
                {
                    this.WriteComma();
                }

                if (expr == paramArg)
                {
                    this.WriteOpenBracket();
                }

                needComma = true;
                expr.AcceptVisitor(this.Emitter);

                if (this.Emitter.Writers.Count != count)
                {
                    this.PopWriter();
                    count = this.Emitter.Writers.Count;
                }
            }

            if (paramArg != null)
            {
                this.WriteCloseBracket();
            }
        }
    }
}