using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastracture.Models
{
    public class MunchkinHost
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public byte Сapacity { get; set; }
        public byte Fullness { get; set; }
    }
}
