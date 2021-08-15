using System;
using System.Threading;
using LogLib;

namespace LogLibTest
{
    class Program
    {
        static void Main(string[] args)
        {
            NSLog log = new NSLog();
            log.Connect();
            log.Clear();
            log.Print("hello,world!");
            for (int i = 0; i < 8; i++)
            {
                log.Print(i.ToString());
                Thread.Sleep(1000);
            }
            log.Print(1, 1, "###");
            log.Print(" ");
            log.Print(" ");  
            for (int i = 0; i < 8; i++)
            {
                log.Draw(i, 0, '_');
                log.Draw(i, 1, '|');
            }
            Console.ReadKey(true);
        }
    }
}
