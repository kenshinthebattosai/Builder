using ICSharpCode.NRefactory.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bridge.Translator
{
    public class ReferenceArgumentVisitor : DepthFirstAstVisitor
    {
        public ReferenceArgumentVisitor()
        {
            this.DirectionExpression = new List<Expression>();
        }

        public List<Expression> DirectionExpression
        {
            get;
            set;
        }

        public override void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            this.DirectionExpression.Add(directionExpression.Expression);
            base.VisitDirectionExpression(directionExpression);
        }
    }
}
