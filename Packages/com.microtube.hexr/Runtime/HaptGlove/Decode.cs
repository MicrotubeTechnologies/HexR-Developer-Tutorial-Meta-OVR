using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaptGlove
{
    public class Decode
    {
        public int cur_type;
        public byte[] data;
        private int data_index = 0;
        public int data_length;

        public Decode()
        {
        }

        public Queue GetFrameQueue(byte[] buf, int length)
        {
            Queue FrameQueue = new Queue();
            int index = 0;
            do
            {
                int frame_length = buf[index];
                if (frame_length == 0)
                {
                    break;
                }
                byte[] frame_data = new byte[frame_length];

                Buffer.BlockCopy(buf, index, frame_data, 0, frame_length);
                index += frame_data[0];
                FrameQueue.Enqueue(frame_data);
                //MessageBox.Show(index.ToString());
            } while (index < length);

            return FrameQueue;
        }

        public int GetFunc(byte[] frame_data)
        {
            int func = frame_data[1];
            //MessageBox.Show("收到的数据:" + BitConverter.ToString(frame_data) + "\n" + frame_data.Length);
            return func;
        }

        public Queue GetDataQueue(byte[] frame)//解数据
        {
            Queue DataQueue = new Queue();

            int index = 2;
            int length = frame[0];
            do
            {
                index += get_data_q(frame, index, DataQueue);
            } while (index < length);

            return DataQueue;
        }

        int get_data_q(byte[] buf, int index, Queue DataQueue)
        {
            data_length = get_length(buf[index]);
            data = new byte[data_length];
            Buffer.BlockCopy(buf, index, data, 0, data_length);
            object buf_data = get_data(data);
            DataQueue.Enqueue(buf_data);
            return data_length;
        }


        object get_data(byte[] buf)
        {
            data_index = 0;
            //MessageBox.Show(buf[0].ToString());
            switch (buf[0])
            {
                case 1:
                    return get_u8();
                case 2:
                    return get_u16();
                case 3:
                    return get_u32();
                case 4:
                    return get_u64();
                case 5:
                    return get_i8();
                case 6:
                    return get_i16();
                case 7:
                    return get_i32();
                case 8:
                    return get_i64();
                case 9:
                    return get_f32();
                case 10:
                    return get_d64();
                case 11:
                    return get_b1();
                default:
                    return 0;
            }
        }

        public int get_length(int type)
        {
            switch (type)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 5;
                case 4:
                    return 9;
                case 5:
                    return 2;
                case 6:
                    return 3;
                case 7:
                    return 5;
                case 8:
                    return 9;
                case 9:
                    return 5;
                case 10:
                    return 9;
                case 11:
                    return 2;
                default:
                    return 0;
            }
        }

        public int get_type()
        {
            return data[data_index];
        }

        public string get_type_s()
        {
            cur_type = data[data_index];
            //MessageBox.Show(BitConverter.ToString(raw_data) + "\n" + "raw_index: " + raw_index + "\n" + "cur_type: " + cur_type.ToString());
            switch (cur_type)
            {
                case 1:
                    return "U8";
                case 2:
                    return "U16";
                case 3:
                    return "U32";
                case 4:
                    return "U64";
                case 5:
                    return "I8";
                case 6:
                    return "I16";
                case 7:
                    return "I32";
                case 8:
                    return "I64";
                case 9:
                    return "F32";
                case 10:
                    return "D64";
                case 11:
                    return "B1";
                default:
                    return "NULL";
            }
        }

        public byte get_u8()
        {
            data_index += 2;
            return data[data_index - 1];
        }
        public sbyte get_i8()
        {
            data_index += 2;
            return (sbyte)data[data_index - 1];
        }
        public UInt16 get_u16()
        {
            data_index += 3;
            return BitConverter.ToUInt16(data, data_index - 2);
        }
        public Int16 get_i16()
        {
            data_index += 3;
            return BitConverter.ToInt16(data, data_index - 2);
        }
        public UInt32 get_u32()
        {
            data_index += 5;
            return BitConverter.ToUInt32(data, data_index - 4);
        }
        public Int32 get_i32()
        {
            data_index += 5;
            return BitConverter.ToInt32(data, data_index - 4);
        }
        public UInt64 get_u64()
        {
            data_index += 9;
            return BitConverter.ToUInt64(data, data_index - 8);
        }
        public Int64 get_i64()
        {
            data_index += 9;
            return BitConverter.ToInt64(data, data_index - 8);
        }
        public float get_f32()
        {
            data_index += 5;
            return BitConverter.ToSingle(data, data_index - 4);
        }
        public double get_d64()
        {
            data_index += 9;
            //MessageBox.Show(BitConverter.ToDouble(data, raw_index - 8).ToString());
            return BitConverter.ToDouble(data, data_index - 8);
        }
        public bool get_b1()
        {
            data_index += 2;
            return BitConverter.ToBoolean(data, data_index - 1);
        }
    }
}
