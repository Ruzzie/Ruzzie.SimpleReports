using System;
using System.Runtime.CompilerServices;

namespace TinyToml.Scanning
{
    internal ref struct SourceScanState
    {
        public int CurrentPos => _currentPos;

        private int _currentPos;

        public int StartIndex;
        public int Line;
        public int Column;

        public readonly ReadOnlySpan<char> SourceDataSpan;
        public readonly int                SourceDataLength;

        public NumberScope NumberScope;
        public ScanScope   ScanScope;

        public SourceScanState(ReadOnlySpan<char> input)
        {
            _currentPos      = 0;
            SourceDataLength = input.Length;
            SourceDataSpan   = input;
            StartIndex       = 0;
            Line             = 1;
            Column           = 0;
            NumberScope      = NumberScope.None;
            ScanScope        = ScanScope.Key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAtEnd()
        {
            return _currentPos >= SourceDataLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNext()
        {
            return !IsAtEnd();
        }

        public char Advance()
        {
            if (!IsAtEnd())
            {
                return SourceDataSpan[_currentPos++];
            }

            return '\0'; //NULL CHAR
        }

        public void Advance(int count)
        {
            if (!IsAtEnd())
            {
                var newIndex = _currentPos + count;
                if (newIndex <= SourceDataLength)
                {
                    _currentPos = newIndex;
                }
            }
        }

        public ReadOnlySpan<char> LookAhead(int count)
        {
            if (_currentPos + count <= SourceDataLength)
            {
                return SourceDataSpan.Slice(_currentPos, count);
            }

            return ReadOnlySpan<char>.Empty;
        }

        public char PeekNext()
        {
            if (_currentPos + 1 <= SourceDataLength)
            {
                return SourceDataSpan[_currentPos];
            }

            return '\0';
        }

        public char PeekNextUnsafe()
        {
            return SourceDataSpan[_currentPos];
        }

        public void BeginNumberScope(NumberScope scope)
        {
            NumberScope = scope;
        }

        public void EndNumberScope()
        {
            NumberScope = NumberScope.None;
        }
    }
}