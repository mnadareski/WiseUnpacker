#if NETCOREAPP

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SabreTools.Serialization.Wrappers
{
    /// <summary>
    /// Serializer class for interfaces
    /// </summary>
    /// <see href="https://stackoverflow.com/a/72775719"/>
    internal class ConcreteInterfaceSerializer : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsInterface;

        class ConcreteInterfaceSerializerOfType<TInterface> : JsonConverter<TInterface>
        {
            static ConcreteInterfaceSerializerOfType()
            {
                if (!typeof(TInterface).IsAbstract && !typeof(TInterface).IsInterface)
                    throw new NotImplementedException(string.Format("Concrete class {0} is not supported", typeof(TInterface)));
            }

#if NETCOREAPP3_1
            public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotImplementedException();
#else
            public override TInterface? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotImplementedException();
#endif

            public override void Write(System.Text.Json.Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options) =>
                JsonSerializer.Serialize<object>(writer, value!, options);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(ConcreteInterfaceSerializerOfType<>).MakeGenericType(new Type[] { type }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: Array.Empty<object>(),
                culture: null).ThrowOnNull();
    }
}

#endif
