using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            WiseUnpacker.WiseUnpacker h = new WiseUnpacker.WiseUnpacker();
            h.ExtractTo(args[0], args[1]);
            Console.WriteLine("Extracted");
            Console.ReadKey();
        }
    }
}
