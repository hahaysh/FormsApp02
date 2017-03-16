using System;

namespace NetCoreSample1
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //NetCoreLibrarySample1.NetCoreClass netCoreClass = new NetCoreLibrarySample1.NetCoreClass();

            Console.WriteLine("Hello .Net Core World~!");
            Console.WriteLine("Enter two numbers.");

            Console.WriteLine("First : ");
            var strTemp = Convert.ToInt16(Console.ReadLine());

            Console.WriteLine("Second : ");
            var strImsi = Convert.ToInt16(Console.ReadLine());

            Console.WriteLine("Result : ");
            Console.WriteLine(AddIntToString(strTemp, strImsi));
            Console.ReadLine();
        }

        static string AddIntToString(int x, int y) => (x + y).ToString();
    }
}