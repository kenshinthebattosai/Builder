using Bridge.Contract;
using ICSharpCode.NRefactory.CSharp;
using System;
using System.Collections.Generic;

namespace Bridge.Translator
{
    public class TypeInfo : ITypeInfo
    {
        public TypeInfo() 
        {
            this.StaticMethods = new Dictionary<string, List<MethodDeclaration>>();
            this.InstanceMethods = new Dictionary<string, List<MethodDeclaration>>();
            this.StaticProperties = new Dictionary<string, List<EntityDeclaration>>();
            this.InstanceProperties = new Dictionary<string, List<EntityDeclaration>>();
            this.FieldsDeclarations = new Dictionary<string, FieldDeclaration>();
            this.EventsDeclarations = new Dictionary<string, EventDeclaration>();            
            this.Dependencies = new List<IPluginDependency>();
            this.Ctors = new List<ConstructorDeclaration>();
            this.Operators = new Dictionary<OperatorType, List<OperatorDeclaration>>();

            this.StaticConfig = new TypeConfigInfo();
            this.InstanceConfig = new TypeConfigInfo();
        }

        public TypeConfigInfo StaticConfig
        {
            get;
            set;
        }

        public TypeConfigInfo InstanceConfig
        {
            get;
            set;
        }

        public Dictionary<OperatorType, List<OperatorDeclaration>> Operators
        {
            get;
            protected set;
        }

        public Dictionary<string, EventDeclaration> EventsDeclarations
        {
            get;
            set;
        }

        public TypeDeclaration TypeDeclaration
        {
            get;
            set;
        }

        public bool IsStatic 
        { 
            get; 
            set; 
        }

        public ClassType ClassType 
        { 
            get; 
            set; 
        }
        
        public string Namespace 
        { 
            get; 
            set; 
        }

        public string Name 
        { 
            get; 
            set; 
        }

        public string ParentName
        {
            get
            {
                if (this.ParentType == null)
                {
                    return this.Name;
                }

                return this.ParentType.Name + "." + this.Name;
            }
        }

        public string ParentGenericName
        {
            get
            {
                if (this.ParentType == null)
                {
                    return this.GenericName;
                }

                return (this.ParentType.GenericName ?? this.ParentType.Name) + "." + this.GenericName;
            }
        }

        public HashSet<string> Usings 
        { 
            get; 
            set; 
        }
        
        public List<ConstructorDeclaration> Ctors
        { 
            get; 
            set; 
        }

        public ConstructorDeclaration StaticCtor
        {
            get;
            set;
        }

        public Dictionary<string, FieldDeclaration> FieldsDeclarations
        {
            get;
            protected set;
        }

        public Dictionary<string, List<MethodDeclaration>> StaticMethods 
        { 
            get; 
            protected set; 
        }

        public Dictionary<string, List<MethodDeclaration>> InstanceMethods 
        { 
            get; 
            protected set; 
        }

        public Dictionary<string, List<EntityDeclaration>> StaticProperties
        {
            get;
            protected set;
        }

        public Dictionary<string, List<EntityDeclaration>> InstanceProperties
        {
            get;
            protected set;
        }

        public bool HasStatic
        {
            get
            {
                return this.StaticConfig.HasMembers
                       || this.StaticMethods.Count > 0 
                       || this.StaticProperties.Count > 0                       
                       || this.StaticCtor != null
                       || this.Operators.Count > 0;
            }
        }

        public bool HasInstantiable
        {
            get
            {
                return this.InstanceConfig.HasMembers
                       || this.InstanceMethods.Count > 0
                       || this.InstanceProperties.Count > 0 
                       || this.Ctors.Count > 0;
            }
        }

        public string FullName
        {
            get 
            {
                if (String.IsNullOrEmpty(this.Namespace))
                {
                    return this.ParentName;
                }
                return this.Namespace + "." + this.ParentName;
            }
        }

        public string GenericFullName
        {
            get
            {
                if (String.IsNullOrEmpty(this.Namespace))
                {
                    return this.ParentGenericName;
                }
                return this.Namespace + "." + this.ParentGenericName;
            }
        }

        private int enumValue = 0;

        public virtual int LastEnumValue
        {
            get
            {
                return enumValue++;
            }
        }

        public bool IsEnum 
        { 
            get; 
            set; 
        }

        public string GenericName 
        { 
            get; set; 
        }

        public string FileName
        {
            get;
            set;
        }

        public string Module
        {
            get;
            set;
        }

        public List<IPluginDependency> Dependencies
        {
            get;
            set;
        }

        public ITypeInfo ParentType
        {
            get;
            set;
        }
    }
}
