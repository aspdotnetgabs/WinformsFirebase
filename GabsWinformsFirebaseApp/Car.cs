using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabsWinformsFirebaseApp
{
    class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
    }

    class CarType
    {
        public string Id { get; set; }
        public string Type { get; set; }
    }

    class CarIdGenerator
    {
        public int Id { get; set; }
    }
}
