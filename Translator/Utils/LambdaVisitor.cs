using ICSharpCode.NRefactory.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Translator
{
    public class LambdaVisitor : DepthFirstAstVisitor
    {
        public LambdaVisitor()
        {
            this.LambdaExpression = new List<Expression>();
        }

        public List<Expression> LambdaExpression
        {
            get;
            set;
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
        {
            this.LambdaExpression.Add(anonymousMethodExpression);
            base.VisitAnonymousMethodExpression(anonymousMethodExpression);
        }

        public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            this.LambdaExpression.Add(lambdaExpression);
            base.VisitLambdaExpression(lambdaExpression);
        }
    }
}
