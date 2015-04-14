using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using System.Threading.Tasks;
using AustinHarris.JsonRpc;
namespace RobinaSpeechServer
{
    class Program
    {
        static object[] services = new object[]
        {
            new SpeechHandler()
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Start");
            (services[0] as SpeechHandler).init();
            (services[0] as SpeechHandler).Start();


            Console.ReadLine();
            Console.ReadLine();
        }
    }
}
