using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Bridge.Contract;

namespace Bridge.Translator
{
    public class IndexerBlock : AbstractEmitterBlock
    {
        public IndexerBlock(IEmitter emitter, IndexerExpression indexerExpression)
        {
            this.Emitter = emitter;
            this.IndexerExpression = indexerExpression;
        }

        public IndexerExpression IndexerExpression 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            this.VisitIndexerExpression();
        }

        protected void VisitIndexerExpression()
        {
            IndexerExpression indexerExpression = this.IndexerExpression;

            IAttribute inlineAttr = null;
            var resolveResult = this.Emitter.Resolver.ResolveNode(indexerExpression, this.Emitter);
            var isIgnore = false;
            var ignoreAccessor = false;
            IProperty member = null;
            IMethod method = null;

            if (resolveResult is MemberResolveResult)
            {
                var resolvedMember = ((MemberResolveResult)resolveResult).Member;
                isIgnore = this.Emitter.Validator.IsIgnoreType(resolvedMember.DeclaringTypeDefinition);

                if (resolvedMember is IProperty)
                {
                    member = (IProperty)resolvedMember;
                    method = this.Emitter.IsAssignment ? member.Setter : member.Getter;
                    inlineAttr = this.Emitter.GetAttribute(method.Attributes, Translator.Bridge_ASSEMBLY + ".TemplateAttribute");
                    ignoreAccessor = this.Emitter.Validator.HasAttribute(method.Attributes, "Bridge.IgnoreAttribute");
                }
            }

            if (inlineAttr != null || isIgnore)
            {
                indexerExpression.Target.AcceptVisitor(this.Emitter);
            }

            if (inlineAttr != null)
            {
                var inlineCode = inlineAttr.PositionalArguments[0];
                if (inlineCode.ConstantValue != null)
                {
                    string code = inlineCode.ConstantValue.ToString();

                    this.WriteDot();
                    this.PushWriter(code);
                    new ExpressionListBlock(this.Emitter, indexerExpression.Arguments, null).Emit();

                    if (!this.Emitter.IsAssignment)
                    {
                        this.PopWriter();
                    }
                    else
                    {
                        this.WriteComma();
                    }
                }
            }
            else if (!isIgnore)
            {
                var oldIsAssignment = this.Emitter.IsAssignment;
                this.Emitter.IsAssignment = false;
                indexerExpression.Target.AcceptVisitor(this.Emitter);
                this.Emitter.IsAssignment = oldIsAssignment;
                this.WriteDot();
                var argsInfo = new ArgumentsInfo(this.Emitter, indexerExpression);
                var argsExpressions = argsInfo.ArgumentsExpressions;
                var paramsArg = argsInfo.ParamsExpression;
                var name = Helpers.GetPropertyRef(member, this.Emitter, this.Emitter.IsAssignment);

                if (!this.Emitter.IsAssignment)
                {
                    this.Write(name);
                    this.WriteOpenParentheses();
                    new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                    this.WriteCloseParentheses();
                }
                else
                {
                    this.Write(name);
                    this.WriteOpenParentheses();
                    new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                    this.PushWriter(", {0})");
                }
            }
            else
            {
                if (indexerExpression.Arguments.Count != 1)
                {
                    throw (Exception)this.Emitter.CreateException(indexerExpression, "Only one index is supported");
                }

                var index = indexerExpression.Arguments.First();

                var primitive = index as PrimitiveExpression;

                if (primitive != null && primitive.Value != null && Regex.Match(primitive.Value.ToString(), "^[_$a-z][_$a-z0-9]*$", RegexOptions.IgnoreCase).Success)
                {
                    this.WriteDot();
                    this.Write(primitive.Value);
                }
                else
                {
                    this.WriteOpenBracket();
                    index.AcceptVisitor(this.Emitter);
                    this.WriteCloseBracket();
                }
            }
        }
    }
}
