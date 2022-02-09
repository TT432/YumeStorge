using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YumeStorge
{
    [AttributeUsage(AttributeTargets.Field
        | AttributeTargets.Property,
        AllowMultiple = true)]
    public class YumeAttribute : Attribute
    {
        private string name;

        public YumeAttribute(string name)
        {
            this.name = name;
        }

        public string Name { get { return name; } }
    }
}
