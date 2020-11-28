﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public class ExecutionSet
    {
        private readonly ArduinoCsCompiler _compiler;
        private readonly List<ArduinoMethodDeclaration> _methods;
        private readonly List<Class> _classes;
        private readonly Dictionary<TypeInfo, int> _patchedTypeTokens;
        private readonly Dictionary<MethodBase, int> _patchedMethodTokens;
        private readonly Dictionary<FieldInfo, int> _patchedFieldTokens;

        private int _numDeclaredMethods;
        private ArduinoTask _entryPoint;
        private int _nextToken;

        public ExecutionSet(ArduinoCsCompiler compiler)
        {
            _compiler = compiler;
            _methods = new List<ArduinoMethodDeclaration>();
            _classes = new List<Class>();
            _patchedTypeTokens = new Dictionary<TypeInfo, int>();
            _patchedMethodTokens = new Dictionary<MethodBase, int>();
            _patchedFieldTokens = new Dictionary<FieldInfo, int>();
            _nextToken = 1;

            _numDeclaredMethods = 0;
            _entryPoint = null!;
            MainEntryPointInternal = null;
        }

        public IList<Class> Classes => _classes;

        public ArduinoTask EntryPoint
        {
            get => _entryPoint;
            internal set => _entryPoint = value;
        }

        internal MethodInfo? MainEntryPointInternal
        {
            get;
            set;
        }

        public void Load()
        {
            if (MainEntryPointInternal == null)
            {
                throw new InvalidOperationException("Main entry point not defined");
            }

            // TODO: This should not be necessary later
            _compiler.ClearAllData(true);
            _compiler.SendClassDeclarations(this);
            _compiler.SendMethods(this);

            EntryPoint = _compiler.GetTask(this, MainEntryPointInternal);

            // Execute all static ctors
            _compiler.ExecuteStaticCtors(this);
        }

        public long EstimateRequiredMemory()
        {
            long bytesUsed = 0;
            foreach (var cls in Classes)
            {
                bytesUsed += 20;
                bytesUsed += cls.StaticSize;
                bytesUsed += cls.Members.Count * 8; // Assuming 32 bit target system for now
            }

            foreach (var method in _methods)
            {
                bytesUsed += 12;
                bytesUsed += method.ArgumentCount * 8;
                if (method.TokenMap != null)
                {
                    bytesUsed += method.TokenMap.Count;
                }

                if (method.IlBytes != null)
                {
                    bytesUsed += method.IlBytes.Length;
                }
            }

            return bytesUsed;
        }

        public int GetOrAddMethodToken(MethodBase methodBase)
        {
            int token;
            if (_patchedMethodTokens.TryGetValue(methodBase, out token))
            {
                return token;
            }

            token = _nextToken++;
            _patchedMethodTokens.Add(methodBase, token);
            return token;
        }

        public int GetOrAddFieldToken(FieldInfo field)
        {
            int token;
            if (_patchedFieldTokens.TryGetValue(field, out token))
            {
                return token;
            }

            token = _nextToken++;
            _patchedFieldTokens.Add(field, token);
            return token;
        }

        public int GetOrAddClassToken(TypeInfo typeInfo)
        {
            int token;
            if (_patchedTypeTokens.TryGetValue(typeInfo, out token))
            {
                return token;
            }

            token = _nextToken++;
            _patchedTypeTokens.Add(typeInfo, token);
            return token;
        }

        public bool AddClass(Class type)
        {
            if (_classes.Any(x => x.Cls == type.Cls))
            {
                return false;
            }

            _classes.Add(type);
            return true;
        }

        public bool HasDefinition(Type classType)
        {
            if (_classes.Any(x => x.Cls == classType))
            {
                return true;
            }

            return false;
        }

        public bool HasMethod(MemberInfo m)
        {
            return _methods.Any(x => x.MethodBase == m);
        }

        public bool AddMethod(ArduinoMethodDeclaration method)
        {
            if (_numDeclaredMethods >= Math.Pow(2, 14) - 1)
            {
                // In practice, the maximum will be much less on most Arduino boards, due to ram limits
                throw new NotSupportedException("To many methods declared. Only 2^14 supported.");
            }

            if (_methods.Any(x => x.MethodBase == method.MethodBase))
            {
                return false;
            }

            _methods.Add(method);
            method.Index = _numDeclaredMethods;
            _numDeclaredMethods++;

            return true;
        }

        public IEnumerable<ArduinoMethodDeclaration> Methods()
        {
            return _methods;
        }

        public MemberInfo? InverseResolveToken(int token)
        {
            // Todo: This is very slow - consider inversing the dictionaries during data prep
            var method = _patchedMethodTokens.FirstOrDefault(x => x.Value == token);
            if (method.Key != null)
            {
                return method.Key;
            }

            var field = _patchedFieldTokens.FirstOrDefault(x => x.Value == token);
            if (field.Key != null)
            {
                return field.Key;
            }

            var t = _patchedTypeTokens.FirstOrDefault(x => x.Value == token);
            if (t.Key != null)
            {
                return t.Key;
            }

            return null;
        }

        public class VariableOrMethod
        {
            public VariableOrMethod(VariableKind variableType, int token, List<int> baseTokens)
            {
                VariableType = variableType;
                Token = token;
                BaseTokens = baseTokens;
                InitialValue = null;
            }

            public VariableKind VariableType
            {
                get;
            }

            public int Token
            {
                get;
            }

            public List<int> BaseTokens
            {
                get;
            }

            public object? InitialValue
            {
                get;
                set;
            }
        }

        public class Class
        {
            private bool _suppressInit;

            public Class(Type cls, int dynamicSize, int staticSize, List<VariableOrMethod> members)
            {
                Cls = cls;
                DynamicSize = dynamicSize;
                StaticSize = staticSize;
                Members = members;
            }

            public Type Cls
            {
                get;
            }

            public int DynamicSize { get; }
            public int StaticSize { get; }

            public List<VariableOrMethod> Members
            {
                get;
                internal set;
            }

            public bool SuppressInit
            {
                get { return _suppressInit; }
                set { _suppressInit = value; }
            }
        }
    }
}
