using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaptGlove
{
    public class Encode
    {
        static Encode _instance;
        public static Encode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Encode();
                }
                return _instance;
            }
        }

        public List<byte> list = new List<byte>();

        public void clear_list()
        {
            list.Clear();
            //list.Add(0x02);
        }

        public byte[] add_fun(byte n)
        {
            byte[] b = new byte[2];
            b[0] = Convert.ToByte(list.Count + 2);
            b[1] = n;
            list.Insert(0, b[1]);
            list.Insert(0, b[0]);
            return list.ToArray();
        }

        public byte[] add_u8(byte n)// Input a byte, output an encoded byte[]
        {
            byte[] b = new byte[2];
            b[0] = 0x01;
            b[1] = n;
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_u16(UInt16 n)// Input an unsigned int16, output an encoded byte[]
        {
            byte[] b = new byte[3];
            b[0] = 0x02;
            b[1] = (byte)(n % 256);
            b[2] = (byte)(n / 256);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_u32(UInt32 n)//输入unsigned int32，输出编码好的byte[] //数据的低字节存到高地址
        {
            byte[] b = new byte[5];
            b[0] = 0x03;
            for (int i = 1; i < 5; i++)
            {
                b[i] = (byte)(n % 256);
                n = n / 256;
            }
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_u64(UInt64 n)//输入unsigned int64，输出编码好的byte[]
        {
            byte[] b = new byte[9];
            b[0] = 0x04;
            for (int i = 1; i < 9; i++)
            {
                b[i] = (byte)(n % 256);
                n = n / 256;
            }
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_i8(sbyte n)//输入sbyte，输出编码好的byte[]
        {
            byte[] b = new byte[2];
            b[0] = 0x05;
            b[1] = (byte)n;
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_i16(Int16 n)//输入int16，输出编码好的byte[]
        {
            byte[] b = new byte[3];
            b[0] = 0x06;
            byte[] tem = new byte[2];
            tem = BitConverter.GetBytes(n);
            Buffer.BlockCopy(tem, 0, b, 1, 2);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_i32(Int32 n)//输入int32，输出编码好的byte[]
        {
            byte[] b = new byte[5];
            b[0] = 0x07;
            byte[] tem = new byte[4];
            tem = BitConverter.GetBytes(n);
            Buffer.BlockCopy(tem, 0, b, 1, 4);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_i64(Int64 n)//输入int64，输出编码好的byte[]
        {
            byte[] b = new byte[9];
            b[0] = 0x08;
            byte[] tem = new byte[8];
            tem = BitConverter.GetBytes(n);
            Buffer.BlockCopy(tem, 0, b, 1, 8);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_f32(float n)//输入float，输出编码好的byte[]
        {
            byte[] b = new byte[5];
            b[0] = 0x09;
            byte[] tem = new byte[4];
            tem = BitConverter.GetBytes(n);
            Buffer.BlockCopy(tem, 0, b, 1, 4);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_d64(double n)//输入double，输出编码好的byte[]
        {
            byte[] b = new byte[9];
            b[0] = 0x0a;
            byte[] tem = new byte[8];
            tem = BitConverter.GetBytes(n);
            Buffer.BlockCopy(tem, 0, b, 1, 8);
            list.AddRange(b);
            return list.ToArray();
        }
        public byte[] add_b1(bool n)//输入bool，输出编码好的byte[]
        {
            byte[] b = new byte[2];
            b[0] = 0x0b;
            if (n)
            {
                b[1] = 0x01;
            }
            else
            {
                b[1] = 0x00;
            }
            list.AddRange(b);
            return list.ToArray();
        }
    }
}
