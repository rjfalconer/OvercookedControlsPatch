using System;
using System.Linq;
using Mono.Cecil;

namespace OvercookedControlsPatcher
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AddMethod : Attribute
    {
        public readonly string targetType;

        public AddMethod(string targetType)
        {
            this.targetType = targetType;
        }

        public static AddMethod Read(MethodDefinition method)
        {
            var attr = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == nameof(AddMethod));
            if (attr == null)
            {
                return null;
            }
            var targetType = (string)attr.ConstructorArguments[0].Value;
            var val = new AddMethod(targetType);
            return val;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaceMethod : Attribute
    {
        public readonly string targetType;

        public ReplaceMethod(string targetType)
        {
            this.targetType = targetType;
        }

        public static ReplaceMethod Read(MethodDefinition method)
        {
            var attr = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == nameof(ReplaceMethod));
            if (attr == null)
            {
                return null;
            }
            var targetType = (string)attr.ConstructorArguments[0].Value;
            var val = new ReplaceMethod(targetType);
            return val;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AddField : Attribute
    {
        public readonly string targetType;

        public AddField(string targetType)
        {
            this.targetType = targetType;
        }

        public static AddField Read(FieldDefinition field)
        {
            var attr = field.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == nameof(AddField));
            if (attr == null)
            {
                return null;
            }
            var targetType = (string)attr.ConstructorArguments[0].Value;
            var val = new AddField(targetType);
            return val;
        }
    }
}
