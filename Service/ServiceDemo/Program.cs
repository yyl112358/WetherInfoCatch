using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Service service = new Service();
            service.StartService("http://localhost:3953/", "8A1F86548CDD406D880F631507650159");
            Console.Read();
        }
    }
}
