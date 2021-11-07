using System.Runtime.CompilerServices;
using TinyToml.Scanning;
using TinyToml.Types;

namespace TinyToml.Parsing
{
    internal ref struct ParseState
    {
        public Token Current;
        public Token Previous;

        public ParseError LastError;

        public readonly TomlTable DocRoot;

        public SourceScanState ScanState;
        public TomlTable       CurrentTable;
        public string          CurrentKey;
        public TomlArray       CurrentArray;

        private ScopeStack _scopeStack;

        public ParseState(ref SourceScanState scanState, TomlTable docRoot)
        {
            ScanState    = scanState;
            DocRoot      = docRoot;

            _scopeStack  = new ScopeStack();

            Current      = default;
            Previous     = default;
            LastError    = ParseError.Empty;
            CurrentTable = docRoot;
            CurrentKey   = "";
            CurrentArray = new TomlArray("", 0);
        }

        public Scope CurrentScope
        {
            get
            {
                var (_, scope) = _scopeStack.TryPeek();
                return scope;
            }
        }

        internal enum Scope : byte
        {
            None,
            Array,
            Table
        }

        public void BeginArrayScope(TomlArray array)
        {
            _scopeStack.TryPush(Scope.Array);
            CurrentArray = array;
        }

        public bool EndArrayScope(TomlArray previousArrayScopeToRestore)
        {
            var result = EndScope(Scope.Array);
            if (result)
                CurrentArray = previousArrayScopeToRestore;
            return result;
        }

        public void BeginTableScope(TomlTable table)
        {
            _scopeStack.TryPush(Scope.Table);
            CurrentTable = table;
        }

        public bool EndTableScope(TomlTable previousTableScopeToRestore)
        {
            var result = EndScope(Scope.Table);
            if (result)
                CurrentTable = previousTableScopeToRestore;
            return result;
        }

        private bool EndScope(Scope scopeType)
        {
            if (CurrentScope == scopeType)
            {
                var (success, _) = _scopeStack.TryPop();
                return success;
            }
            return false;
        }
    }

    internal ref struct ScopeStack
    {
        // ReSharper disable once InconsistentNaming
        private const        int  MAX_BUFFER_SIZE = 12;
#pragma warning disable 649
        private unsafe fixed byte _buffer[MAX_BUFFER_SIZE];
#pragma warning restore 649

        private int _size;

        public int Count    => _size;
        public int Capacity => MAX_BUFFER_SIZE;

        public (bool success, ParseState.Scope result) TryPeek()
        {
            var newSize = _size - 1;

            if (IsOutOfBounds(newSize))
            {
                return (false, default);
            }

            unsafe
            {
                return (true, (ParseState.Scope) _buffer[newSize]);
            }
        }

        public bool TryPush(ParseState.Scope value)
        {
            var currentSize = _size;

            if (IsOutOfBounds(currentSize))
            {
                return false;
            }

            unsafe
            {
                fixed (byte* ptr = &_buffer[_size++])
                {
                    *ptr = (byte) value;
                }
            }

            return true;
        }

        public (bool success, ParseState.Scope result) TryPop()
        {
            var newSize = _size - 1; // _size is always the next index, so the top item index is _size -1

            if (IsOutOfBounds(newSize))
            {
                return (false, default);
            }

            _size = newSize;

            unsafe
            {
                var result = _buffer[newSize]; //copy item to result
                //_buffer[newSize] = default;    //clear item?

                return (true, (ParseState.Scope) result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOutOfBounds(int index)
        {
            //cast to uint to perform valid unchecked comparison (also for the case -1)
            // ReSharper disable once RedundantCast
            return (uint) index >= (uint) MAX_BUFFER_SIZE;
        }
    }
}