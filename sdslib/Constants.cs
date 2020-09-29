namespace sdslib
{
    public static class Constants
    {
        public static class SdsHeader
        {
            public const int StandardHeaderSize = 72;
            public const uint MaxSupportedVersion = 20U;
            public const uint Unknown32_C = 1610314995U;
            public const uint Unknown32_2C = 1U;
            public const ulong Uknown64_38 = 0UL;
            public const int BlockSize = 16384;
            public const uint Encrypted = 1049068U;
        }

        public static class Resource
        {
            public const int StandardHeaderSizeV19 = 30;
            public const int StandardHeaderSizeV20 = 38;
        }
    }
}
