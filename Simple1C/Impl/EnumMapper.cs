﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    internal class EnumMapper
    {
        private readonly GlobalContext globalContext;

        private static readonly ConcurrentDictionary<Type, MapItem[]> mappings =
            new ConcurrentDictionary<Type, MapItem[]>();

        private readonly object enumerations;

        public EnumMapper(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
            enumerations = ComHelpers.GetProperty(globalContext.ComObject(), "Перечисления");
        }

        public object MapFrom1C(Type enumType, object value1C)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            var valueIndex = Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf", value1C));
            var result = mappings.GetOrAdd(enumType, GetMappings)
                .SingleOrDefault(x => x.index == valueIndex);
            if (result == null)
            {
                const string messageFormat = "can't map value [{0}] to enum [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    globalContext.String(value1C), enumType.Name));
            }
            return result.value;
        }

        public object MapTo1C(object value)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, value.GetType().Name);
            return ComHelpers.GetProperty(enumeration, value.ToString());
        }

        public object MapTo1C(int valueIndex, Type enumType)
        {
            var enumValue = Enum.GetValues(enumType).GetValue(valueIndex);
            return MapTo1C(enumValue);
        }

        private MapItem[] GetMappings(Type enumType)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(v => new MapItem
                {
                    value = v,
                    index =
                        Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf",
                            ComHelpers.GetProperty(enumeration, v.ToString())))
                })
                .ToArray();
        }

        private class MapItem
        {
            public object value;
            public int index;
        }
    }
}