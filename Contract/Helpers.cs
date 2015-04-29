using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Globalization;

namespace Bridge.Contract
{
    public static partial class Helpers 
    {
        public static void AcceptChildren(this AstNode node, IAstVisitor visitor)
        {
            foreach (AstNode child in node.Children)
            {
                child.AcceptVisitor(visitor);
            }
        }

        public static string ReplaceSpecialChars(string name) 
        {
            return name.Replace('`', '$').Replace('/', '.').Replace("+", ".");
        }

        public static bool IsSubclassOf(TypeDefinition thisTypeDefinition, TypeDefinition typeDefinition, IEmitter emitter)
        {
            if (thisTypeDefinition.BaseType != null)
            {
                TypeDefinition baseTypeDefinition = null;

                try
                {
                    baseTypeDefinition = Helpers.ToTypeDefinition(thisTypeDefinition.BaseType, emitter);
                }
                catch { }

                if (baseTypeDefinition != null)
                {
                    return (baseTypeDefinition == typeDefinition || Helpers.IsSubclassOf(baseTypeDefinition, typeDefinition, emitter));
                }
            }
            return false;
        }

        public static bool IsImplementationOf(TypeDefinition thisTypeDefinition, TypeDefinition interfaceTypeDefinition, IEmitter emitter)
        {
            foreach (TypeReference interfaceReference in thisTypeDefinition.Interfaces)
            {
                if (interfaceReference == interfaceTypeDefinition)
                {
                    return true;
                }

                TypeDefinition interfaceDefinition = null;
                
                try 
                {
                    interfaceDefinition = Helpers.ToTypeDefinition(interfaceReference, emitter);
                }
                catch { }

                if (interfaceDefinition != null && Helpers.IsImplementationOf(interfaceDefinition, interfaceTypeDefinition, emitter))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsAssignableFrom(TypeDefinition thisTypeDefinition, TypeDefinition typeDefinition, IEmitter emitter)
        {
            return (thisTypeDefinition == typeDefinition 
                    || (typeDefinition.IsClass && !typeDefinition.IsValueType && Helpers.IsSubclassOf(typeDefinition, thisTypeDefinition, emitter))
                    || (typeDefinition.IsInterface && Helpers.IsImplementationOf(typeDefinition, thisTypeDefinition, emitter)));
        }

        public static TypeDefinition ToTypeDefinition(TypeReference reference, IEmitter emitter)
        {
            if (reference == null)
            {
                return null;
            }

            try
            {
                if (reference.IsGenericInstance)
                {
                    reference = reference.GetElementType();
                }

                string key = BridgeTypes.GetTypeDefinitionKey(reference.FullName);

                if (emitter.TypeDefinitions.ContainsKey(reference.FullName))
                {
                    return emitter.TypeDefinitions[reference.FullName];
                }

                return reference.Resolve();
            }
            catch
            {
            }

            return null;
        }

        public static bool IsIgnoreGeneric(IType type, IEmitter emitter)
        {
            return emitter.Validator.HasAttribute(type.GetDefinition().Attributes, "Bridge.IgnoreGenericAttribute");
        }

        public static bool IsIgnoreCast(AstType astType, IEmitter emitter)
        {
            var typeDef = emitter.BridgeTypes.ToType(astType).GetDefinition();
            return typeDef == null ? false : emitter.Validator.HasAttribute(typeDef.Attributes, "Bridge.IgnoreCastAttribute");
        }

        public static bool IsIntegerType(IType type, IMemberResolver resolver)
        {
            type = type.IsKnownType(KnownTypeCode.NullableOfT) ? ((ParameterizedType)type).TypeArguments[0] : type;

            return type.Equals(resolver.Compilation.FindType(KnownTypeCode.Byte))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.SByte))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Char))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Int16))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.UInt16))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Int32))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.UInt32))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Int64))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.UInt64));
        }

        public static bool IsFloatType(IType type, IMemberResolver resolver)
        {
            type = type.IsKnownType(KnownTypeCode.NullableOfT) ? ((ParameterizedType)type).TypeArguments[0] : type;

            return type.Equals(resolver.Compilation.FindType(KnownTypeCode.Decimal))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Double))
                || type.Equals(resolver.Compilation.FindType(KnownTypeCode.Single));
        }

        public static void CheckValueTypeClone(ResolveResult resolveResult, Expression expression, IAbstractEmitterBlock block)
        {
            if (resolveResult == null || resolveResult.IsError)
            {
                return;
            }

            if (resolveResult is InvocationResolveResult ||
               block.Emitter.IsAssignment)
            {
                return;
            }

            if (resolveResult.Type.Kind == TypeKind.Struct)
            {
                var typeDef = block.Emitter.GetTypeDefinition(resolveResult.Type);
                if (block.Emitter.Validator.IsIgnoreType(typeDef))
                {
                    return;
                }

                var memberResult = resolveResult as MemberResolveResult;

                var field = memberResult != null ? memberResult.Member as DefaultResolvedField : null;

                if (field != null && field.IsReadOnly)
                {
                    block.Write(".$clone()");
                    return;
                }

                if (expression == null ||
                    expression.Parent is NamedExpression ||
                    expression.Parent is ObjectCreateExpression ||
                    expression.Parent is ArrayInitializerExpression || 
                    expression.Parent is ReturnStatement || 
                    expression.Parent is InvocationExpression || 
                    expression.Parent is AssignmentExpression ||
                    expression.Parent is VariableInitializer)
                {
                    block.Write(".$clone()");
                }
            }
        }

        public static bool IsAutoProperty(PropertyDefinition propDef)
        {
            var typeDef = propDef.DeclaringType;
            if (propDef != null)
            {
                if (propDef.GetMethod == null || propDef.SetMethod == null)
                {
                    return false;
                }
                if (propDef.GetMethod.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                {
                    return true;
                }
            }
            return typeDef.Fields.Any(f => !f.IsPublic && !f.IsStatic && f.Name.Contains("BackingField") && f.Name.Contains("<" + propDef.Name + ">"));
        }
        public static bool IsFieldProperty(IMember property, IEmitter emitter)
        {
            bool isAuto = property.Attributes.Any(a => a.AttributeType.FullName == "Bridge.FieldPropertyAttribute");            
            if (!isAuto && emitter.AssemblyInfo.AutoPropertyToField)
            {
                var typeDef = emitter.GetTypeDefinition(property.DeclaringType);
                var propDef = typeDef.Properties.FirstOrDefault(p => p.Name == property.Name);
                return Helpers.IsAutoProperty(propDef);
            }
            return isAuto;
        }

        public static bool IsFieldProperty(IMemberDefinition property, IEmitter emitter)
        {
            bool isAuto = property.CustomAttributes.Any(a => a.AttributeType.FullName == "Bridge.FieldPropertyAttribute");
            if (!isAuto && emitter.AssemblyInfo.AutoPropertyToField)
            {
                return Helpers.IsAutoProperty((PropertyDefinition)property);
            }
            return isAuto;
        }
        public static bool IsFieldProperty(PropertyDeclaration property, IEmitter emitter)
        {            
            string name = "Bridge.FieldProperty";
            string name1 = name + "Attribute";
            foreach (var i in property.Attributes)
            {
                foreach (var j in i.Attributes)
                {
                    if (j.Type.ToString() == name || j.Type.ToString() == name1)
                    {
                        return true;
                    }
                    var resolveResult = emitter.Resolver.ResolveNode(j, emitter);
                    if (resolveResult != null && resolveResult.Type != null && resolveResult.Type.FullName == name1)
                    {
                        return true;
                    }
                }
            }
            if (!emitter.AssemblyInfo.AutoPropertyToField) 
            {
                return false;
            }
            var typeDef = emitter.GetTypeDefinition();
            var propDef = typeDef.Properties.FirstOrDefault(p => p.Name == property.Name);
            return Helpers.IsAutoProperty(propDef);
        }
        public static string GetEventRef(CustomEventDeclaration property, IEmitter emitter, bool remove = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var name = emitter.GetEntityName(property, true, ignoreInterface);

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, remove);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
            }

            return (remove ? "remove" : "add") + name;
        }
        public static string GetEventRef(IMember property, IEmitter emitter, bool remove = false, bool noOverload = false, bool ignoreInterface = false)
            {
            var name = emitter.GetEntityName(property, true, ignoreInterface);

            if (!noOverload)
                {
                var overloads = OverloadsCollection.Create(emitter, property, remove);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
                }
            return (remove ? "remove" : "add") + name;
            }

        public static string GetPropertyRef(PropertyDeclaration property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var name = emitter.GetEntityName(property, true, ignoreInterface);            

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
            }

            if (Helpers.IsFieldProperty(property, emitter))
            {
                return noOverload ? emitter.GetEntityName(property, false) : name;
            }

            return (isSetter ? "set" : "get") + name;
        }

        public static string GetPropertyRef(IndexerDeclaration property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var name = emitter.GetEntityName(property, true, ignoreInterface);

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
            }

            return (isSetter ? "set" : "get") + name;
        }

        public static string GetIndexerRef(IMember property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var name = emitter.GetEntityName(property, true, ignoreInterface);

            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
            }

            return (isSetter ? "set" : "get") + name;
        }

        public static string GetPropertyRef(IMember property, IEmitter emitter, bool isSetter = false, bool noOverload = false, bool ignoreInterface = false)
        {
            var name = emitter.GetEntityName(property, true, ignoreInterface);            
            
            if (!noOverload)
            {
                var overloads = OverloadsCollection.Create(emitter, property, isSetter);
                name = overloads.HasOverloads ? overloads.GetOverloadName() : name;
                noOverload = !overloads.HasOverloads;
            }

            if (Helpers.IsFieldProperty(property, emitter))
            {

                return noOverload ? emitter.GetEntityName(property, false) : name;
            }

            return (isSetter ? "set" : "get") + name;
        }

        public static List<MethodDefinition> GetMethods(TypeDefinition typeDef, IEmitter emitter, List<MethodDefinition> list = null)
        {
            if (list == null)
            {
                list = new List<MethodDefinition>(typeDef.Methods);
            }
            else
            {
                list.AddRange(typeDef.Methods);
            }

            var baseTypeDefinition = Helpers.ToTypeDefinition(typeDef.BaseType, emitter);

            if (baseTypeDefinition != null)
            {
                Helpers.GetMethods(baseTypeDefinition, emitter, list);
            }

            return list;
        }
        private static readonly string[] reservedWords = new string[] { "abstract", "arguments", "as", "boolean", "break", "byte", "case", "catch", "char", "class", "continue", "const", "constructor", "debugger", "default", "delete", "do", "double", "else", "enum", "eval", "export", "extends", "false", "final", "finally", "float", "for", "function", "goto", "if", "implements", "import", "in", "instanceof", "int", "interface", "is", "let", "long", "namespace", "native", "new", "null", "package", "private", "protected", "public", "return", "short", "static", "super", "switch", "synchronized", "this", "throw", "throws", "transient", "true", "try", "typeof", "use", "var", "void", "volatile", "while", "with", "yield" };

        public static bool IsReservedWord(string word)
        {
            return reservedWords.Contains(word);
        }

        public static object GetEnumValue(IEmitter emitter, IType type, object constantValue)
        {
            var enumMode = emitter.Validator.EnumEmitMode(type);

            if ((emitter.Validator.IsIgnoreType(type.GetDefinition()) && enumMode == -1) || enumMode == 2)
            {
                return constantValue;
            }

            if (enumMode >= 3)
            {
                var member = type.GetFields().FirstOrDefault(f => f.ConstantValue == constantValue);

                if (member == null)
                {
                    return constantValue;
                }                
                
                string enumStringName = member.Name;
                var attr = emitter.GetAttribute(member.Attributes, "Bridge.NameAttribute");

                if (attr != null)
                {
                    enumStringName = emitter.GetEntityName(member);
                }
                else
                {
                    switch (enumMode)
                    {
                        case 3:
                            enumStringName = member.Name.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + member.Name.Substring(1);
                            break;
                        case 4:
                            break;
                        case 5:
                            enumStringName = enumStringName.ToLowerInvariant();
                            break;
                        case 6:
                            enumStringName = enumStringName.ToUpperInvariant();
                            break;
                    }
                }

                return enumStringName;
            }

            return constantValue;
        }
    }
}