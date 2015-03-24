using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using System.Linq;
using Bridge.Contract;

namespace Bridge.Translator
{
    public partial class AbstractEmitterBlock
    {
        public virtual void PushLocals()
        {
            if (this.Emitter.LocalsStack == null)
            {
                this.Emitter.LocalsStack = new Stack<Dictionary<string, AstType>>();
            }

            this.Emitter.LocalsStack.Push(this.Emitter.Locals);
            this.Emitter.Locals = new Dictionary<string, AstType>(this.Emitter.Locals);
        }

        public virtual void PopLocals()
        {
            this.Emitter.Locals = this.Emitter.LocalsStack.Pop();
        }

        public virtual void ResetLocals()
        {
            this.Emitter.Locals = new Dictionary<string, AstType>();
            this.Emitter.IteratorCount = 0;
        }

        public virtual void AddLocals(IEnumerable<ParameterDeclaration> declarations, AstNode statement)
        {
            declarations.ToList().ForEach(item =>
            {
                var vName = this.AddLocal(item.Name, item.Type);                

                if (item.ParameterModifier == ParameterModifier.Out || item.ParameterModifier == ParameterModifier.Ref)
                {
                    this.Emitter.LocalsMap[item.Name] = vName + ".v";
                }
                else
                {
                    this.Emitter.LocalsMap[item.Name] = vName;
                }
            });

            var visitor = new ReferenceArgumentVisitor();
            statement.AcceptVisitor(visitor);

            foreach (IdentifierExpression expr in visitor.DirectionExpression)
            {
                this.Emitter.LocalsMap[expr.Identifier] = expr.Identifier + ".v";
            }
        }

        public string AddLocal(string name, AstType type)
        {
            this.Emitter.Locals.Add(name, type);            

            if (this.Emitter.IsAsync && !this.Emitter.AsyncVariables.Contains(name))
            {
                this.Emitter.AsyncVariables.Add(name);
            }

            name = name.StartsWith(Bridge.Translator.Emitter.FIX_ARGUMENT_NAME) ? name.Substring(Bridge.Translator.Emitter.FIX_ARGUMENT_NAME.Length) : name;
            string vName = name;
            if (Helpers.IsReservedWord(name))
            {
                vName = this.GetUniqueName(name);
            }

            if (!this.Emitter.LocalsNamesMap.ContainsKey(name))
            {
                this.Emitter.LocalsNamesMap.Add(name, vName);
            }
            else
            {
                this.Emitter.LocalsNamesMap[name] = this.GetUniqueName(vName);
            }

            return this.Emitter.LocalsNamesMap[name];
        }

        protected virtual string GetUniqueName(string name)
        {
            string tempName = name + "1";
            while (this.Emitter.LocalsNamesMap.ContainsValue(tempName))
            {
                tempName += "1";
            }

            return tempName;
        }

        public virtual Dictionary<string, string> BuildLocalsMap()
        {            
            var prevMap = this.Emitter.LocalsMap;
            if (prevMap == null)
            {
                this.Emitter.LocalsMap = new Dictionary<string, string>();
            }
            else
            {
                this.Emitter.LocalsMap = new Dictionary<string, string>(prevMap);
            }            

            return prevMap;
        }

        public virtual void ClearLocalsMap(Dictionary<string, string> prevMap = null)
        {
            this.Emitter.LocalsMap = prevMap;
        }

        public virtual Dictionary<string, string> BuildLocalsNamesMap()
        {
            var prevMap = this.Emitter.LocalsNamesMap;

            if (prevMap == null)
            {
                this.Emitter.LocalsNamesMap = new Dictionary<string, string>();
            }
            else
            {
                this.Emitter.LocalsNamesMap = new Dictionary<string, string>(prevMap);
            }            

            return prevMap;
        }

        public virtual void ClearLocalsNamesMap(Dictionary<string, string> prevMap = null)
        {
            this.Emitter.LocalsNamesMap = prevMap;
        }

        public virtual void ConvertParamsToReferences(IEnumerable<ParameterDeclaration> declarations)
        {
            declarations.ToList().ForEach(item =>
            {
                var isReferenceLocal = this.Emitter.LocalsMap.ContainsKey(item.Name) && this.Emitter.LocalsMap[item.Name].EndsWith(".v");
                if (isReferenceLocal && !(item.ParameterModifier == ParameterModifier.Out || item.ParameterModifier == ParameterModifier.Ref))
                {
                    this.Write(string.Format("{0} = {{v:{0}}};", this.Emitter.LocalsNamesMap[item.Name]));
                    this.WriteNewLine();
                }
            });
        }
    }
}
