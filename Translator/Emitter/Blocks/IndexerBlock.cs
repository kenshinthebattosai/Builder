using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using System.Text.RegularExpressions;

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
            var memberResolveResult = resolveResult as MemberResolveResult;

            var arrayAccess = resolveResult as ArrayAccessResolveResult;

            if (arrayAccess != null && arrayAccess.Indexes.Count > 1)
            {
                this.EmitArrayAccess(indexerExpression);
                return;
            }

            var isIgnore = true;
            var ignoreAccessor = false;
            IProperty member = null;
            IMethod method = null;
            var oldIsAssignment = this.Emitter.IsAssignment;
            var oldUnary = this.Emitter.IsUnaryAccessor;

            if (memberResolveResult != null)
            {
                var resolvedMember = memberResolveResult.Member;
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
                this.Emitter.IsAssignment = false;
                this.Emitter.IsUnaryAccessor = false;
                indexerExpression.Target.AcceptVisitor(this.Emitter);
                this.Emitter.IsAssignment = oldIsAssignment;
                this.Emitter.IsUnaryAccessor = oldUnary;
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
            else if (!(isIgnore || ignoreAccessor))
            {
                string targetVar = null;
                string valueVar = null;
                bool writeTargetVar = false;
                bool isStatement = false;

                if (this.Emitter.IsAssignment && this.Emitter.AssignmentType != AssignmentOperatorType.Assign)
                {
                    writeTargetVar = true;
                }
                else if (this.Emitter.IsUnaryAccessor)
                {
                    writeTargetVar = true;

                    isStatement = indexerExpression.Parent is UnaryOperatorExpression && indexerExpression.Parent.Parent is ExpressionStatement;

                    if (memberResolveResult != null && NullableType.IsNullable(memberResolveResult.Type))
                    {
                        isStatement = false;
                    }

                    if (!isStatement)
                    {
                        this.WriteOpenParentheses();
                    }
                }

                if (writeTargetVar) 
                {
                    var targetrr = this.Emitter.Resolver.ResolveNode(indexerExpression.Target, this.Emitter);
                    var memberTargetrr = targetrr as MemberResolveResult;
                    bool isField = memberTargetrr != null && memberTargetrr.Member is IField && (memberTargetrr.TargetResult is ThisResolveResult || memberTargetrr.TargetResult is LocalResolveResult);
                    
                    if (!(targetrr is ThisResolveResult || targetrr is LocalResolveResult || isField))
                    {
                        targetVar = this.GetTempVarName();                        
                        this.Write(targetVar);
                        this.Write(" = ");
                    }
                }

                if (this.Emitter.IsUnaryAccessor && !isStatement && targetVar == null)
                {
                    valueVar = this.GetTempVarName();

                    this.Write(valueVar);
                    this.Write(" = ");
                }

                this.Emitter.IsAssignment = false;
                this.Emitter.IsUnaryAccessor = false;
                indexerExpression.Target.AcceptVisitor(this.Emitter);
                this.Emitter.IsAssignment = oldIsAssignment;
                this.Emitter.IsUnaryAccessor = oldUnary;

                if (targetVar != null)
                {
                    if (this.Emitter.IsUnaryAccessor && !isStatement)
                    {
                        this.WriteComma(false);

                        valueVar = this.GetTempVarName();

                        this.Write(valueVar);
                        this.Write(" = ");

                        this.Write(targetVar);
                    }
                    else
                    {
                        this.WriteSemiColon();
                        this.WriteNewLine();
                        this.Write(targetVar);
                    }
                }          

                this.WriteDot();
                var argsInfo = new ArgumentsInfo(this.Emitter, indexerExpression);
                var argsExpressions = argsInfo.ArgumentsExpressions;
                var paramsArg = argsInfo.ParamsExpression;
                var name = Helpers.GetPropertyRef(member, this.Emitter, this.Emitter.IsAssignment);

                if (!this.Emitter.IsAssignment)
                {
                    if (this.Emitter.IsUnaryAccessor)
                    {
                        var oldWriter = this.SaveWriter();
                        this.NewWriter();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        var paramsStr = this.Emitter.Output.ToString();
                        this.RestoreWriter(oldWriter);

                        if (isStatement)
                        {
                            this.Write(Helpers.GetPropertyRef(member, this.Emitter, true));
                            this.WriteOpenParentheses();
                            this.Write(paramsStr);
                            this.WriteComma(false);

                            if (targetVar != null)
                            {
                                this.Write(targetVar);
                            }
                            else
                            {
                                indexerExpression.Target.AcceptVisitor(this.Emitter);
                            }

                            this.WriteDot();

                            this.Write(Helpers.GetPropertyRef(member, this.Emitter, false));
                            this.WriteOpenParentheses();
                            this.Write(paramsStr);                            
                            this.WriteCloseParentheses();

                            if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.PostIncrement)
                            {
                                this.Write("+");
                            }
                            else
                            {
                                this.Write("-");
                            }

                            this.Write("1");
                            this.WriteCloseParentheses();
                        }
                        else
                        {
                            this.Write(Helpers.GetPropertyRef(member, this.Emitter, false));
                            this.WriteOpenParentheses();
                            this.Write(paramsStr);                            
                            this.WriteCloseParentheses();
                            this.WriteComma();

                            if (targetVar != null)
                            {
                                this.Write(targetVar);
                            }
                            else
                            {
                                indexerExpression.Target.AcceptVisitor(this.Emitter);
                            }
                            this.WriteDot();
                            this.Write(Helpers.GetPropertyRef(member, this.Emitter, true));
                            this.WriteOpenParentheses();
                            this.Write(paramsStr);
                            this.WriteComma(false);
                            this.Write(valueVar);

                            if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.PostIncrement)
                            {
                                this.Write("+");
                            }
                            else
                            {
                                this.Write("-");
                            }

                            this.Write("1");
                            this.WriteCloseParentheses();
                            this.WriteComma();

                            this.Write(valueVar);

                            if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.Decrement)
                            {
                                if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment)
                                {
                                    this.Write("+");
                                }
                                else
                                {
                                    this.Write("-");
                                }

                                this.Write("1");
                            }

                            this.WriteCloseParentheses();

                            if (valueVar != null)
                            {
                                this.RemoveTempVar(valueVar);
                            }
                        }

                        if (targetVar != null)
                        {
                            this.RemoveTempVar(targetVar);
                        }
                    }
                    else
                    {
                        this.Write(name);
                        this.WriteOpenParentheses();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.WriteCloseParentheses();
                    }
                }
                else
                {
                    if (this.Emitter.AssignmentType != AssignmentOperatorType.Assign)
                    {
                        var oldWriter = this.SaveWriter();
                        this.NewWriter();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        var paramsStr = this.Emitter.Output.ToString();
                        this.RestoreWriter(oldWriter);

                        if (targetVar != null)
                        {
                            this.PushWriter(string.Concat(
                                name,
                                "(",
                                paramsStr,
                                ", ",
                                targetVar,
                                ".",
                                Helpers.GetPropertyRef(member, this.Emitter, false),
                                "(",
                                paramsStr,
                                "){0})"));

                            this.RemoveTempVar(targetVar);
                        }
                        else
                        {
                            oldWriter = this.SaveWriter();
                            this.NewWriter();

                            this.Emitter.IsAssignment = false;
                            this.Emitter.IsUnaryAccessor = false;
                            indexerExpression.Target.AcceptVisitor(this.Emitter);
                            this.Emitter.IsAssignment = oldIsAssignment;
                            this.Emitter.IsUnaryAccessor = oldUnary;

                            var trg = this.Emitter.Output.ToString();

                            this.RestoreWriter(oldWriter);
                            this.PushWriter(string.Concat(
                                name,
                                "(",
                                paramsStr,
                                ", ",
                                trg,
                                ".",
                                Helpers.GetPropertyRef(member, this.Emitter, false),
                                "(",
                                paramsStr,
                                "){0})"));
                        }
                    }
                    else
                    {
                        this.Write(name);
                        this.WriteOpenParentheses();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.PushWriter(", {0})");
                    }
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

        protected virtual void EmitArrayAccess(IndexerExpression indexerExpression)
        {
            string targetVar = null;
            bool writeTargetVar = false;
            bool isStatement = false;
            string valueVar = null;
            var resolveResult = this.Emitter.Resolver.ResolveNode(indexerExpression, this.Emitter);

            if (this.Emitter.IsAssignment && this.Emitter.AssignmentType != AssignmentOperatorType.Assign)
            {
                writeTargetVar = true;
            }
            else if (this.Emitter.IsUnaryAccessor)
            {
                writeTargetVar = true;

                isStatement = indexerExpression.Parent is UnaryOperatorExpression && indexerExpression.Parent.Parent is ExpressionStatement;

                if (NullableType.IsNullable(resolveResult.Type))
                {
                    isStatement = false;
                }

                if (!isStatement)
                {
                    this.WriteOpenParentheses();
                }
            }

            if (writeTargetVar)
            {
                var targetrr = this.Emitter.Resolver.ResolveNode(indexerExpression.Target, this.Emitter);
                var memberTargetrr = targetrr as MemberResolveResult;
                bool isField = memberTargetrr != null && memberTargetrr.Member is IField && (memberTargetrr.TargetResult is ThisResolveResult || memberTargetrr.TargetResult is LocalResolveResult);
                
                if (!(targetrr is ThisResolveResult || targetrr is LocalResolveResult || isField))
                {
                    targetVar = this.GetTempVarName();

                    this.Write(targetVar);
                    this.Write(" = ");
                }
            }

            if (this.Emitter.IsUnaryAccessor && !isStatement && targetVar == null)
            {
                valueVar = this.GetTempVarName();

                this.Write(valueVar);
                this.Write(" = ");
            }

            var oldIsAssignment = this.Emitter.IsAssignment;
            var oldUnary = this.Emitter.IsUnaryAccessor;

            this.Emitter.IsAssignment = false;
            this.Emitter.IsUnaryAccessor = false;
            indexerExpression.Target.AcceptVisitor(this.Emitter);
            this.Emitter.IsAssignment = oldIsAssignment;
            this.Emitter.IsUnaryAccessor = oldUnary;

            if (targetVar != null)
            {
                if (this.Emitter.IsUnaryAccessor && !isStatement)
                {
                    this.WriteComma(false);

                    valueVar = this.GetTempVarName();

                    this.Write(valueVar);
                    this.Write(" = ");

                    this.Write(targetVar);
                }
                else
                {
                    this.WriteSemiColon();
                    this.WriteNewLine();
                    this.Write(targetVar);
                }
            }   

            this.WriteDot();

            var argsInfo = new ArgumentsInfo(this.Emitter, indexerExpression);
            var argsExpressions = argsInfo.ArgumentsExpressions;
            var paramsArg = argsInfo.ParamsExpression;

            if (!this.Emitter.IsAssignment)
            {
                if (this.Emitter.IsUnaryAccessor)
                {
                    if (isStatement)
                    {
                        this.Write("set");
                        this.WriteOpenParentheses();
                        this.WriteOpenBracket();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.WriteCloseBracket();
                        this.WriteComma(false);

                        if (targetVar != null)
                        {
                            this.Write(targetVar);
                        }
                        else
                        {
                            indexerExpression.Target.AcceptVisitor(this.Emitter);
                        }

                        this.WriteDot();

                        this.Write("get");
                        this.WriteOpenParentheses();
                        this.WriteOpenBracket();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.WriteCloseBracket();
                        this.WriteCloseParentheses();

                        if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.PostIncrement)
                        {
                            this.Write("+");
                        }
                        else
                        {
                            this.Write("-");
                        }

                        this.Write("1");
                        this.WriteCloseParentheses();
                    }
                    else
                    {
                        this.Write("get");
                        this.WriteOpenParentheses();
                        this.WriteOpenBracket();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.WriteCloseBracket();
                        this.WriteCloseParentheses();
                        this.WriteComma();

                        if (targetVar != null)
                        {
                            this.Write(targetVar);
                        }
                        else
                        {
                            indexerExpression.Target.AcceptVisitor(this.Emitter);
                        }
                        this.WriteDot();
                        this.Write("set");
                        this.WriteOpenParentheses();
                        this.WriteOpenBracket();
                        new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                        this.WriteCloseBracket();
                        this.WriteComma(false);
                        this.Write(valueVar);

                        if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.PostIncrement)
                        {
                            this.Write("+");
                        }
                        else
                        {
                            this.Write("-");
                        }

                        this.Write("1");
                        this.WriteCloseParentheses();
                        this.WriteComma();

                        this.Write(valueVar);

                        if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment || this.Emitter.UnaryOperatorType == UnaryOperatorType.Decrement)
                        {
                            if (this.Emitter.UnaryOperatorType == UnaryOperatorType.Increment)
                            {
                                this.Write("+");
                            }
                            else
                            {
                                this.Write("-");
                            }

                            this.Write("1");
                        }

                        this.WriteCloseParentheses();

                        if (valueVar != null)
                        {
                            this.RemoveTempVar(valueVar);
                        }
                    }

                    if (targetVar != null)
                    {
                        this.RemoveTempVar(targetVar);
                    }
                }
                else
                {
                    this.Write("get");
                    this.WriteOpenParentheses();
                    this.WriteOpenBracket();
                    new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                    this.WriteCloseBracket();
                    this.WriteCloseParentheses();
                }
            }
            else
            {
                if (this.Emitter.AssignmentType != AssignmentOperatorType.Assign)
                {
                    var oldWriter = this.SaveWriter();
                    this.NewWriter();
                    new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                    var paramsStr = this.Emitter.Output.ToString();
                    this.RestoreWriter(oldWriter);

                    if (targetVar != null)
                    {
                        this.PushWriter(string.Concat(
                            "set([",
                            paramsStr,
                            "],",
                            targetVar,
                            ".get([",
                            paramsStr,
                            "]){0})"), () => { this.RemoveTempVar(targetVar); });                        
                    }
                    else
                    {
                        oldWriter = this.SaveWriter();
                        this.NewWriter();

                        this.Emitter.IsAssignment = false;
                        this.Emitter.IsUnaryAccessor = false;
                        indexerExpression.Target.AcceptVisitor(this.Emitter);
                        this.Emitter.IsAssignment = oldIsAssignment;
                        this.Emitter.IsUnaryAccessor = oldUnary;

                        var trg = this.Emitter.Output.ToString();

                        this.RestoreWriter(oldWriter);
                        this.PushWriter(string.Concat(
                            "set([",
                            paramsStr,
                            "],",
                            trg,
                            ".get([",
                            paramsStr,
                            "]){0})"));
                    }
                }
                else
                {
                    this.Write("set");
                    this.WriteOpenParentheses();
                    this.WriteOpenBracket();
                    new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg).Emit();
                    this.WriteCloseBracket();
                    this.PushWriter(", {0})");
                }
            }
        }
    }
}