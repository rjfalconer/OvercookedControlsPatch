using Mono.Cecil;
using Mono.Cecil.Cil;

record FieldPatch(string TypeName, string FieldName);
record MethodPatch(string TypeName, string MethodName, int ParameterCount);
record ConstructorPatch(string TypeName);
record MethodStub(string TypeName, string MethodName, string ReturnType, string[] ParameterTypes);


class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            Console.WriteLine("Usage: GeneratePublicAssembly <Assembly-CSharp.dll>");
            return;
        }

        var inputPath = args[0];
        var outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath) + "-mod.dll");
        var assembly = AssemblyDefinition.ReadAssembly(inputPath, new ReaderParameters { ReadWrite = false });
        var module = assembly.MainModule;

        var fieldsToPatch = new[]
        {
            new FieldPatch("PCPadInputProvider", "m_allDevices"),
            new FieldPatch("StandardActionSet", "m_pveValueActions"),
            new FieldPatch("StandardActionSet", "m_nveValueActions")
        };

        var methodsToPatch = new[]
        {
            new MethodPatch("StandardActionSet", "ResetActions", 0),
            new MethodPatch("StandardActionSet", "LoadControlsFromFile", 2)
        };

        var ctorsToPatch = new[]
        {
            new ConstructorPatch("StandardActionSet")
        };
        
        var methodStubs = new[]
        {
            new MethodStub("StandardActionSet", "LoadControlsFromFile", "System.Boolean", new[]
            {
                "StandardActionSet",
                "System.String"
            })
        };

        foreach (var patch in fieldsToPatch)
            MakeFieldPublic(module, patch);
        foreach (var patch in methodsToPatch)
            MakeMethodPublic(module, patch);
        foreach (var patch in ctorsToPatch)
            MakeCtorPublic(module, patch);
        foreach (var stub in methodStubs)
            AddMethodStub(module, stub);

        assembly.Write(outputPath);
        Console.WriteLine($"Wrote modified assembly to: {outputPath}");
    }

    static void MakeFieldPublic(ModuleDefinition module, FieldPatch patch)
    {
        var type = module.Types.FirstOrDefault(t => t.Name == patch.TypeName);
        var field = type?.Fields.FirstOrDefault(f => f.Name == patch.FieldName);
        if (field != null)
        {
            Console.WriteLine($"Making {patch.TypeName}.{patch.FieldName} public");
            field.IsPrivate = false;
            field.IsFamily = false;
            field.IsPublic = true;
        }
    }

    static void MakeMethodPublic(ModuleDefinition module, MethodPatch patch)
    {
        var type = module.Types.FirstOrDefault(t => t.Name == patch.TypeName);
        var method = type?.Methods.FirstOrDefault(m => m.Name == patch.MethodName && m.Parameters.Count == patch.ParameterCount);
        if (method != null)
        {
            Console.WriteLine($"Making {patch.TypeName}.{patch.MethodName} public");
            method.IsPrivate = false;
            method.IsFamily = false;
            method.IsPublic = true;
        }
    }

    static void MakeCtorPublic(ModuleDefinition module, ConstructorPatch patch)
    {
        var type = module.Types.FirstOrDefault(t => t.Name == patch.TypeName);
        var ctor = type?.Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 0);
        if (ctor != null)
        {
            Console.WriteLine($"Making {patch.TypeName}.ctor public");
            ctor.IsPrivate = false;
            ctor.IsFamily = false;
            ctor.IsPublic = true;
        }
    }

    static void AddMethodStub(ModuleDefinition module, MethodStub stub)
    {
        var type = module.Types.FirstOrDefault(t => t.Name == stub.TypeName);
        if (type == null)
        {
            Console.WriteLine($"Could not find type: {stub.TypeName}");
            return;
        }

        if (type.Methods.Any(m => m.Name == stub.MethodName && m.Parameters.Count == stub.ParameterTypes.Length))
        {
            Console.WriteLine($"Stub for {stub.TypeName}.{stub.MethodName} already exists");
            return;
        }

        var returnType = module.ImportReference(typeof(bool)); // TODO define and use a stub.ReturnType if we ever need to stub more methods

        var method = new MethodDefinition(
            stub.MethodName,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType
        );

        foreach (var paramTypeName in stub.ParameterTypes)
        {
            TypeReference paramType;

            if (paramTypeName.StartsWith("System."))
            {
                // BCL type
                var sysType = Type.GetType(paramTypeName, throwOnError: true);
                paramType = module.ImportReference(sysType);
            }
            else
            {
                // Game-defined type
                var paramTypeDef = module.Types.FirstOrDefault(t => t.Name == paramTypeName);
                if (paramTypeDef == null)
                {
                    Console.WriteLine($"Warning: Could not find type {paramTypeName} for parameter of {stub.MethodName}");
                    continue;
                }
                paramType = paramTypeDef;
            }

            method.Parameters.Add(new ParameterDefinition(paramType));
        }

        // Add IL body: return false;
        var il = method.Body.GetILProcessor();
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ret));

        type.Methods.Add(method);
        Console.WriteLine($"Stubbed {stub.TypeName}.{stub.MethodName}()");
    }
}
