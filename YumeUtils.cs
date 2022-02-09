using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YumeStorge
{
    public class YumeUtils
    {
        public static YumeUtils Instance = new YumeUtils();

        public readonly Dictionary<int, Func<IYumeElement>> YumeElements;
        public readonly Dictionary<Type, int> YumeTypes;

        private YumeUtils()
        {
            YumeElements = Init();
            YumeTypes = TypesInit();
        }

        public void ToObject(YumeRoot root, object obj)
        {
            Type type = obj.GetType();

            // 遍历属性
            foreach (PropertyInfo pi in type.GetProperties())
            {
                foreach (Attribute a in pi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya && root.Has(ya.Name) && pi.CanRead && pi.CanWrite)
                    {
                        pi.SetValue(obj, pi.PropertyType.IsSubclassOf(typeof(IYumeElement)) ? 
                            root.Get(ya.Name): root.Get(ya.Name).Get());
                    }
                }
            }

            // 遍历字段
            foreach (FieldInfo fi in type.GetFields())
            {
                foreach (Attribute a in fi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya && root.Has(ya.Name))
                    {
                        fi.SetValue(obj, fi.FieldType.IsSubclassOf(typeof(IYumeElement)) ?
                            root.Get(ya.Name) : root.Get(ya.Name).Get());
                    }
                }
            }
        }

        public YumeRoot FormObject(object obj)
        {
            Type type = obj.GetType();

            YumeRoot result = new YumeRoot();

            // 遍历属性
            foreach (PropertyInfo pi in type.GetProperties())
            {
                foreach (Attribute a in pi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya && pi.CanRead && pi.CanWrite)
                    {
                        TryAdd(result, ya.Name, pi.GetValue(obj));
                    }
                }
            }

            // 遍历字段
            foreach (FieldInfo fi in type.GetFields())
            {
                foreach (Attribute a in fi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya)
                    {
                        TryAdd(result, ya.Name, fi.GetValue(obj));
                    }
                }
            }

            return result;
        }

        public IYumeElement GetType(byte type)
        {
            if (YumeElements.ContainsKey(type))
            {
                return YumeElements[type].Invoke();
            }

            return null;
        }

        public void TryAdd(YumeRoot result, string name, object o)
        {
            if (o == null)
            {
                return;
            }

            if (o is IYumeElement ele)
            {
                result.Set(name, ele);
            }
            else
            {
                foreach (var entry in YumeTypes)
                {
                    if (entry.Key.IsInstanceOfType(o))
                    {
                        var value = YumeElements[entry.Value].Invoke();
                        value.Set(o);
                        result.Set(name, value);
                        return;
                    }
                }
            }
        }

        private Dictionary<int, Func<IYumeElement>> Init()
        {
            Dictionary<int, Func<IYumeElement>> result = new Dictionary<int, Func<IYumeElement>>();
            result.Add(1, () => new YumeRoot());
            result.Add(2, () => new YumeInt());
            result.Add(3, () => new YumeDouble());
            result.Add(4, () => new YumeString());
            result.Add(5, () => new YumeArray());
            return result;
        }

        private Dictionary<Type, int> TypesInit()
        {
            Dictionary<Type, int> result = new Dictionary<Type,int>();
            result.Add(typeof(int), 2);
            result.Add(typeof(double), 3);
            result.Add(typeof(string), 4);
            result.Add(typeof(List<>), 5);
            return result;
        }
    }
}
