using System;
using System.Text;
using System.Threading.Tasks;

namespace Sight.Tokenize.Documents
{
    internal class StringDocument : IDocument
    {
        private readonly byte[] _text;
        private int _index;
        private int _current;

        public StringDocument(string text)
        {
            _text = Encoding.UTF8.GetBytes(text);
        }

        public Task<int> ReadAsync(int count)
        {
            if (_index >= _text.Length)
                return Task.FromResult(0);

            _current = _index;
            _index += count;
            var read = _index <= _text.Length ? count : count - (_index - _text.Length);
            return Task.FromResult(read);
        }

        public byte Next()
        {
            if (_current >= _index)
                throw new IndexOutOfRangeException("Index exceeded read byte");

            if (_current < _text.Length)
                return _text[_current++];

            throw new IndexOutOfRangeException("Document size exceeded");
        }
    }
}
