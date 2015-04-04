using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class VariableBlock : AbstractEmitterBlock
    {
        public VariableBlock(IEmitter emitter, VariableDeclarationStatement variableDeclarationStatement)
        {
            this.Emitter = emitter;
            this.VariableDeclarationStatement = variableDeclarationStatement;
        }

        public VariableDeclarationStatement VariableDeclarationStatement 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            this.VisitVariableDeclarationStatement();
        }

        protected virtual void VisitVariableDeclarationStatement()
        {
            bool needVar = true;
            bool needComma = false;
            bool addSemicolon = !this.Emitter.IsAsync;

            var oldSemiColon = this.Emitter.EnableSemicolon;
            var asyncExpressionHandling = this.Emitter.AsyncExpressionHandling;

            foreach (var variable in this.VariableDeclarationStatement.Variables)
            {                
                var varName = this.AddLocal(variable.Name, this.VariableDeclarationStatement.Type);

                if (variable.Initializer != null && !variable.Initializer.IsNull && variable.Initializer.ToString().Contains(Bridge.Translator.Emitter.FIX_ARGUMENT_NAME))                {
                    continue;
                }

                if (needVar)
                {
                    this.WriteVar();
                    needVar = false;
                }

                bool isReferenceLocal = false;

                if (this.Emitter.LocalsMap != null && this.Emitter.LocalsMap.ContainsKey(variable.Name))
                {
                    isReferenceLocal = this.Emitter.LocalsMap[variable.Name].EndsWith(".v");
                }

                this.WriteAwaiters(variable.Initializer);

                var hasInitializer = !variable.Initializer.IsNull;

                if (variable.Initializer.IsNull && !this.VariableDeclarationStatement.Type.IsVar())
                {
                    var typeDef = this.Emitter.GetTypeDefinition(this.VariableDeclarationStatement.Type);

                    if (typeDef.IsValueType && !this.Emitter.Validator.IsIgnoreType(typeDef))
                    {
                        hasInitializer = true;
                    }
                }

                if (!this.Emitter.IsAsync || hasInitializer)
                {
                    if (needComma)
                    {
                        if (this.Emitter.IsAsync)
                        {
                            this.WriteSemiColon();
                        }
                        else
                        {
                            this.WriteComma();
                        }
                    }

                    needComma = true;

                    this.Write(varName);
                }

                if (hasInitializer)
                {
                    addSemicolon = true;
                    this.Write(" = ");

                    if (isReferenceLocal)
                    {
                        this.Write("{ v : ");
                    }

                    var oldValue = this.Emitter.ReplaceAwaiterByVar;
                    this.Emitter.ReplaceAwaiterByVar = true;

                    if (!variable.Initializer.IsNull)
                    {
                        variable.Initializer.AcceptVisitor(this.Emitter);
                    }
                    else
                    {
                        this.Write("new " + Helpers.TranslateTypeReference(this.VariableDeclarationStatement.Type, this.Emitter) + "()");
                    }
                    this.Emitter.ReplaceAwaiterByVar = oldValue;

                    if (isReferenceLocal)
                    {
                        this.Write(" }");
                    }
                }
                else if (isReferenceLocal)
                {
                    addSemicolon = true;
                    this.Write(" = { }");
                }
            }

            this.Emitter.AsyncExpressionHandling = asyncExpressionHandling;

            if (this.Emitter.EnableSemicolon && !needVar && addSemicolon)
            {
                this.WriteSemiColon(true);
            }

            if (oldSemiColon)
            {
                this.Emitter.EnableSemicolon = true;
            }
        }        
    }
}