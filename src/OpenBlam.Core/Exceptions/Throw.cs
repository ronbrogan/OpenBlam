using System;

namespace OpenBlam.Core.Exceptions
{
    public static class Throw
    {
        public static void NotSupported(string message)
        {
            throw new NotSupportedException(message);
        }
    }
}
