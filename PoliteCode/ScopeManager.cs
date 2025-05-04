using System.Collections.Generic;
using System.Linq;

namespace PoliteCode.Core
{
    public class ScopeManager
    {
        private Stack<Dictionary<string, string>> _variableScopes = new Stack<Dictionary<string, string>>();
        private Dictionary<string, string> _functionMap = new Dictionary<string, string>();

        public void PushScope()
        {
            _variableScopes.Push(new Dictionary<string, string>());
        }

        public void PopScope()
        {
            if (_variableScopes.Count > 0)
                _variableScopes.Pop();
        }

        public void DeclareVariable(string name, string type)
        {
            if (_variableScopes.Count == 0)
                PushScope();

            _variableScopes.Peek()[name] = type;
        }

        public bool VariableExists(string name)
        {
            return _variableScopes.Any(scope => scope.ContainsKey(name));
        }

        public bool TryGetVariableType(string name, out string type)
        {
            foreach (var scope in _variableScopes)
            {
                if (scope.TryGetValue(name, out type))
                    return true;
            }

            type = null;
            return false;
        }

        public void DeclareFunction(string name, string returnType)
        {
            _functionMap[name] = returnType;
        }

        public bool FunctionExists(string name) => _functionMap.ContainsKey(name);

        public bool TryGetFunctionReturnType(string name, out string returnType)
            => _functionMap.TryGetValue(name, out returnType);

        public void Reset()
        {
            _variableScopes.Clear();
            _functionMap.Clear();
        }
    }
}
