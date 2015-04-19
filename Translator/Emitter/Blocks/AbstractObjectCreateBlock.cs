using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;

namespace Bridge.Translator
{
    public abstract class AbstractObjectCreateBlock : AbstractEmitterBlock
    {
        protected virtual void WriteObjectInitializer(IEnumerable<Expression> expressions, bool changeCase)
        {
            bool needComma = false;

            foreach (Expression item in expressions)
            {
                NamedExpression namedExression = null;
                NamedArgumentExpression namedArgumentExpression = null;
                IdentifierExpression identifierExpression = null;


                namedExression = item as NamedExpression;
                if (namedExression == null)
                {
                    namedArgumentExpression = item as NamedArgumentExpression;

                    if (namedArgumentExpression == null)
                    {
                        identifierExpression = item as IdentifierExpression;
                    }
                }

                if (needComma)
                {
                    this.WriteComma();
                }

                needComma = true;
                string name;
                Expression expression;

                if (namedExression != null)
                {
                    name = namedExression.Name;
                    expression = namedExression.Expression;
                }
                else if (namedArgumentExpression != null)
                {
                    name = namedArgumentExpression.Name;
                    expression = namedArgumentExpression.Expression;
                }
                else
                {
                    name = identifierExpression.Identifier;
                    expression = identifierExpression;
                }
                
                if (changeCase)
                {
                    name = Object.Net.Utilities.StringUtils.ToLowerCamelCase(name);
                }
                
                this.Write(name, ": ");
                expression.AcceptVisitor(this.Emitter);
            }
        }
    }
}
