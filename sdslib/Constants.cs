namespace sdslib
{
    public static class Constants
    {
        public static class DataTypesSizes
        {
            public const int UInt8 = 1;
            public const int UInt16 = 2;
            public const int UInt32 = 4;
            public const int UInt64 = 8;
        }

        public static class SdsHeader
        {
            public const int StandardHeaderSize = 72;
            public const int ResourceTypeTableOffset = 72;
            public const uint Version = 19U;
            public const uint Unknown32_C = 1610314995U;
            public const uint Unknown32_2C = 1U;
            public const ulong Unknown64_30 = 47524739625783UL;
            public const ulong Uknown64_38 = 0UL;
            public const int BlockSize = 16384; // 16kB
        }
    }
}
