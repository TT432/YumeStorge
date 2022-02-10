using System;
using System.Collections.Generic;
using System.Reflection;

namespace YumeStorge
{
    public class YumeUtils
    {
        private static readonly Lazy<YumeUtils> _instance = new Lazy<YumeUtils>(() => new YumeUtils());
        public static YumeUtils Instance => _instance.Value;

        private Dictionary<byte, Func<IYumeElement>> YumeElements;
        private Dictionary<Type, byte> YumeTypes;
        private Dictionary<Type, byte> MainYumeTypes;

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

            ScanProperties(type, (pi, ya) =>
            {
                if (root.Has(ya.Name))
                {
                    pi.SetValue(obj, pi.PropertyType.IsSubclassOf(typeof(IYumeElement)) ?
                            root.Get(ya.Name) : root.Get(ya.Name).Get());
                }

                return obj;
            });
            

            ScanFields(type, (fi, ya) =>
            {
                if (root.Has(ya.Name))
                {
                    fi.SetValue(obj, fi.FieldType.IsSubclassOf(typeof(IYumeElement)) ?
                                root.Get(ya.Name) : root.Get(ya.Name).Get());
                }
                return obj;
            });
        }

        private void ScanFields(Type type, Func<FieldInfo, YumeAttribute, object> func)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo fi in fields)
            {
                foreach (Attribute a in fi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya)
                    {
                        func.Invoke(fi, ya);
                    }
                }
            }
        }

        private void ScanProperties(Type type, Func<PropertyInfo, YumeAttribute, object> func)
        {
            foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                foreach (Attribute a in pi.GetCustomAttributes(true))
                {
                    if (a is YumeAttribute ya && pi.CanRead && pi.CanWrite)
                    {
                        func(pi, ya);
                    }
                }
            }
        }

        public YumeRoot FormObject(object obj)
        {
            Type type = obj.GetType();

            YumeRoot result = new YumeRoot();

            ScanProperties(type, (pi, ya) =>
            {
                TryAdd(result, ya.Name, pi.GetValue(obj));
                return obj;
            });

            ScanFields(type, (fi, ya) =>
            {
                TryAdd(result, ya.Name, fi.GetValue(obj));
                return obj;
            });

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
                foreach (var key in YumeTypes.Keys)
                {
                    if (key.IsInstanceOfType(o))
                    {
                        IYumeElement value = YumeElements[YumeTypes[key]]();
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
                YumeTypes.Add(subType, id);
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
            RegisterYumeElementType(typeof(YumeBool), typeof(bool), 6, () => new YumeBool());
        }
    }
}
