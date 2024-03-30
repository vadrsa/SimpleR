namespace SimpleR.Protocol.Internal
{
    public static class FrameHelpers
    {
        public const int IntegerLengthEncodedByteCount = 4;
        public const byte IsNotEndOfMessageByte = 0;
        public const byte IsEndOfMessageByte = 1;
    }
}