using Sfs2X.Entities.Data;

namespace LibraServer
{
    public static class Utils
    {

        public static void PutBytes(this ISFSObject sfsObject, string key, byte[] bytes)
        {
            if (bytes != null && bytes.Length != 0)
            {
                if (bytes.Length <= 24000)
                {
                    string text = Convert.ToBase64String(bytes);
                    sfsObject.PutUtfString(key, text);
                    return;
                }
                byte[][] array = BufferSplit(bytes, 24000);
                int num = array.Length;
                string[] array2 = new string[num];
                for (int i = 0; i < num; i++)
                {
                    string text2 = Convert.ToBase64String(array[i]);
                    array2[i] = text2;
                }
                sfsObject.PutUtfStringArray(key, array2);
            }
        }
        private static byte[][] BufferSplit(byte[] buffer, int blockSize)
        {
            byte[][] array = new byte[(buffer.Length + blockSize - 1) / blockSize][];
            int i = 0;
            int num = 0;
            while (i < array.Length)
            {
                array[i] = new byte[Math.Min(blockSize, buffer.Length - num)];
                Array.Copy(buffer, num, array[i], 0, array[i].Length);
                i++;
                num += blockSize;
            }
            return array;
        }
    }
}
