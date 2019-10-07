namespace sdslib
{
    public static class FNV
    {
        public static uint Hash32(byte[] buffer)
        {
            uint hash = 2166136261U;
            for (int i = 0; i < buffer.Length; i++)
            {
                hash *= 16777619u;
                hash ^= (uint)buffer[i];
            }

            return hash;
        }

        public static ulong Hash64(byte[] buffer)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < buffer.Length; i++)
            {
                hash *= 1099511628211UL;
                hash ^= (ulong)buffer[i];
            }

            return hash;
        }
    }
}
