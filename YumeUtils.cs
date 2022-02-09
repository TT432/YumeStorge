using System;
using System.Collections.Generic;
using System.Reflection;

namespace YumeStorge
{
    public class YumeUtils
    {
        public static YumeUtils Instance = new YumeUtils();

        private readonly Dictionary<byte, Func<IYumeElement>> YumeElements;
        private readonly Dictionary<Type, byte> YumeTypes;
        private readonly Dictionary<Type, byte> MainYumeTypes;

        private YumeUtils()
        {
            YumeElements = new Dictionary<byte, Func<IYumeElement>>();
            YumeTypes = new Dictionary<Type, byte>();
            MainYumeTypes = new Dictionary<Type, byte>();

            Register();
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
                            root.Get(ya.Name) : root.Get(ya.Name).Get());
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

        public void RegisterYumeElementType(Type mainType, Type subType, byte id, Func<IYumeElement> func)
        {
            if (YumeElements.ContainsKey(id))
            {
                YumeElements[id] = func;
            }
            else
            {
                YumeElements.Add(id, func);
            }

            if (YumeTypes.ContainsKey(subType))
            {
                YumeTypes[subType] = id;
            }
            else
            {
                YumeElements.Add(id, func);
            }

            if (MainYumeTypes.ContainsKey(mainType))
            {
                MainYumeTypes[mainType] = id;
            }
            else
            {
                MainYumeTypes.Add(mainType, id);
            }
        }

        public byte GetId(IYumeElement yumeElement)
        {
            return MainYumeTypes[yumeElement.GetType()];
        }

        private void Register()
        {
            RegisterYumeElementType(typeof(YumeRoot), typeof(Dictionary<string, IYumeElement>), 1, () => new YumeRoot());
            RegisterYumeElementType(typeof(YumeInt), typeof(int), 2, () => new YumeInt());
            RegisterYumeElementType(typeof(YumeDouble), typeof(double), 3, () => new YumeDouble());
            RegisterYumeElementType(typeof(YumeString), typeof(string), 4, () => new YumeString());
            RegisterYumeElementType(typeof(YumeArray), typeof(List<IYumeElement>), 5, () => new YumeArray());
        }
    }
}
