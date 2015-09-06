using System;

namespace Regex
{
    public class SyntaxException : Exception
    {
        public SyntaxException()
        {
        }

        public SyntaxException(string message)
            : base(message)
        {
        }

        public SyntaxException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
