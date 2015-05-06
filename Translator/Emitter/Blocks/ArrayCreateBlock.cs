using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace Bridge.Translator
{
    public class ArrayCreateBlock : AbstractEmitterBlock
    {
        public ArrayCreateBlock(IEmitter emitter, ArrayCreateExpression arrayCreateExpression) : base(emitter, arrayCreateExpression)
        {
            this.Emitter = emitter;
            this.ArrayCreateExpression = arrayCreateExpression;
        }

        public ArrayCreateExpression ArrayCreateExpression
        {
            get;
            set;
        }

        protected override void DoEmit()
        {
            this.VisitArrayCreateExpression();
        }

        protected void VisitArrayCreateExpression()
        {
            ArrayCreateExpression arrayCreateExpression = this.ArrayCreateExpression;
            var rr = this.Emitter.Resolver.ResolveNode(arrayCreateExpression, this.Emitter) as ArrayCreateResolveResult;
            var at = (ArrayType)rr.Type;
            var rank = arrayCreateExpression.Arguments.Count;

            if (arrayCreateExpression.Initializer.IsNull && rank == 1)
            {
                this.Write("new Array(");
                arrayCreateExpression.Arguments.First().AcceptVisitor(this.Emitter);
                this.Write(")");

                return;
            }

            if (at.Dimensions > 1)
            {
                this.Write("Bridge.Array.create(");
                var defaultInitializer = new PrimitiveExpression(Inspector.GetDefaultFieldValue(arrayCreateExpression.Type, this.Emitter.Resolver), "?");

                if (defaultInitializer == null)
                {
                    this.Write("Bridge.getDefaultValue(" + BridgeTypes.ToJsName(arrayCreateExpression.Type, this.Emitter) + ")");
                }
                else
                {
                    var primitiveExpr = defaultInitializer as PrimitiveExpression;

                    if (primitiveExpr != null && primitiveExpr.Value is AstType)
                    {
                        this.Write("new " + BridgeTypes.ToJsName((AstType)primitiveExpr.Value, this.Emitter) + "()");
                    }
                    else
                    {
                        defaultInitializer.AcceptVisitor(this.Emitter);
                    }
                }

                this.WriteComma();
            }

            if (rr.InitializerElements != null && rr.InitializerElements.Count > 0)
            {
                this.WriteOpenBracket();
                var elements = arrayCreateExpression.Initializer.Elements;
                new ExpressionListBlock(this.Emitter, elements, null).Emit();
                this.WriteCloseBracket();
            }
            else if (at.Dimensions > 1)
            {
                this.Write("null");
            }
            else
            {
                this.Write("[]");
            }

            if (at.Dimensions > 1)
            {
                this.Emitter.Comma = true;

                for (int i = 0; i < rr.SizeArguments.Count; i++)
                {
                    var a = rr.SizeArguments[i];
                    this.EnsureComma(false);

                    if (a.IsCompileTimeConstant)
                    {
                        this.Write(a.ConstantValue);
                    }
                    else if (arrayCreateExpression.Arguments.Count > i)
                    {
                        var arg = arrayCreateExpression.Arguments.ElementAt(i);

                        if (arg != null)
                        {
                            arg.AcceptVisitor(this.Emitter);
                        }
                    }
                    this.Emitter.Comma = true;
                }

                this.Write(")");
                this.Emitter.Comma = false;
            }
        }
    }
}
