using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;

namespace Bridge.Translator
{
    public class BaseReferenceBlock : ConversionBlock
    {
        public BaseReferenceBlock(IEmitter emitter, BaseReferenceExpression baseReferenceExpression)
            : base(emitter, baseReferenceExpression)
        {
            this.Emitter = emitter;
            this.BaseReferenceExpression = baseReferenceExpression;
        }

        public BaseReferenceExpression BaseReferenceExpression 
        { 
            get; 
            set; 
        }

        protected override Expression GetExpression()
        {
            return this.BaseReferenceExpression;
        }

        protected override void EmitConversionExpression()
        {
            var baseType = this.Emitter.GetBaseTypeDefinition();
            this.Write(BridgeTypes.ToJsName(baseType, this.Emitter), ".prototype");
        }
    }
}
