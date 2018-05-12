using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace MultiThreadFileScanner
{
    class Program
    {
        // 根据实际需要 可修改 这些参数
        private const int dataBlockSize = 128; 
        private const string keyword = "微软";

        private const int maxThreadCount = 5;

        private static int runningThreadCount = 0;

        private static object lockObject = new object();

        //***  注意，现在的程序逻辑是 有问题 的
        //***  1 汉字占用的字节(Byte)可能是 2个 或者 3个，如果一个汉字包含的字节跨越了 2个 数据块(DataBlock)，
        //***    那么就会出现乱码，从实际运行的结果看来，确实会出现乱码，但并不会影响全部，所以还是可以观看演示效果。
        //***  2 关键词(keyword)如果 跨越 了 2个 数据块，那么检索的结果就会 不正确。但如果只是观看演示效果的话，影响不大。
        //***    但如果要正式的应用，就需要解决这个问题。
        static void Main(string[] args)
        {
            byte[] arr = null;
            
            ScanPara scanPara = null;

            int seq = 0;
            int readCount;
            int remainLength;

            Console.WriteLine("Scan Begin.");

            using (Stream stream = File.Open("测试文本.txt", FileMode.Open)) 
            {
                while(true)
                {
                    if (runningThreadCount >= maxThreadCount)
                        continue;

                    readCount = dataBlockSize;
                    remainLength = (int)stream.Length - (int)stream.Position;
                    if (readCount > remainLength)
                        readCount = remainLength;

                    if (remainLength <= 0)
                        break;
 
                    arr = new byte[readCount];
                    
                    stream.Read(arr, 0, readCount);

                    scanPara = new ScanPara();
                    scanPara.BlockSeq = seq;
                    scanPara.ByteArray = arr;

                    Task.Factory.StartNew(new Action<object>(Scan), scanPara);

                    seq++;

                    lock (lockObject)
                    {
                        runningThreadCount++;
                    }

                    Console.WriteLine("Main runningThreadCount：" + runningThreadCount.ToString());
                }
                
            }

            while(true)
            {
                if (runningThreadCount == 0)
                    break;

                System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("Scan End.");
            Console.ReadLine();
        }

        private static void Scan(object para)
        {
            

            ScanPara scanPara = (ScanPara)para;

            Console.WriteLine("Thread " + scanPara.BlockSeq.ToString() + " Begin.");

            string str = Encoding.Default.GetString(scanPara.ByteArray);

            Console.WriteLine("Thread " + scanPara.BlockSeq.ToString() + " 要处理的字符串是：\"" + str + "\"");

            int i = 0;
            int j;
            while(true)
            {
                j = str.IndexOf(keyword, i);
                if (j == -1)
                    break;

                Console.WriteLine("Thread " + scanPara.BlockSeq.ToString() + " 发现 keyword \"" + keyword + "\"，数据块：" + scanPara.BlockSeq.ToString() + " 位置：" + j.ToString());
                i = j + keyword.Length;
            }

            Console.WriteLine("Thread " + scanPara.BlockSeq.ToString() + " End.");

            lock (lockObject)
            {
                runningThreadCount--;
            }

            Console.WriteLine("Scan runningThreadCount：" + runningThreadCount.ToString());
        }

        private class ScanPara
        {
            public int BlockSeq { get; set; }
            public byte[] ByteArray { get; set; }
        }
    }
}
