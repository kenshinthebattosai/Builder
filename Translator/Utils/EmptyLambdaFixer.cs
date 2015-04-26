using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Translator
{
    public class EmptyLambdaDetecter : DepthFirstAstVisitor
    {
        public bool Found
        {
            get;
            set;
        }

        public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression.Body.IsNull)
            {
                this.Found = true;                
            }
        }
    }


    public class EmptyLambdaFixer : DepthFirstAstVisitor<AstNode>
    {
        public EmptyLambdaFixer()
        {            
        }

        protected override AstNode VisitChildren(AstNode node)
        {
            List<AstNode> newChildren = null;

            int i = 0;
            foreach (var child in node.Children)
            {
                var newChild = child.AcceptVisitor(this);
                if (newChild != null)
                {
                    newChildren = newChildren ?? Enumerable.Repeat((AstNode)null, i).ToList();
                    newChildren.Add(newChild);
                }
                else if (newChildren != null)
                {
                    newChildren.Add(null);
                }
                i++;
            }

            if (newChildren == null)
                return null;

            var result = node.Clone();

            i = 0;
            foreach (var children in result.Children)
            {
                if (newChildren[i] != null)
                    children.ReplaceWith(newChildren[i]);
                i++;
            }

            return result;
        }

        public override AstNode VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression.Body.IsNull)
            {
                var l = (LambdaExpression)lambdaExpression.Clone();
                l.Body = new IdentifierExpression(lambdaExpression.Parameters.Last().Name);

                return l;
            }

            return lambdaExpression.Clone();
        }
    }
}
