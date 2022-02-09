using System;

namespace YumeStorge
{
    [AttributeUsage(AttributeTargets.Field
        | AttributeTargets.Property)]
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
