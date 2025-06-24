using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace OvercookedControlsPatcher
{
    internal class PatchTools
    {
        /// Modified from https://groups.google.com/forum/#!msg/mono-cecil/uoMLJEZrQ1Q/ewthqjEk-jEJ
        /// <summary>
        /// Copy a method from one module to another.  If the same method exists in the target module, the caller
        /// is responsible to delete it first.
        /// The sourceMethod makes calls to other methods, we divide the calls into two types:
        /// 1. MethodDefinition : these are methods that are defined in the same module as the sourceMethod;
        /// 2. MethodReference : these are methods that are defined in a different module
        /// For type 1 calls, we will copy these MethodDefinitions to the same target typedef.
        /// For type 2 calls, we will not copy the called method
        /// 
        /// Another limitation: any TypeDefinitions that are used in the sourceMethod will not be copied to the target module; a 
        /// typereference is created instead.
        /// </summary>
        /// <param name="copyToTypedef">The typedef to copy the method to</param>
        /// <param name="sourceMethod">The method to copy</param>
        /// <returns></returns>
        public static MethodDefinition CopyMethod(TypeDefinition copyToTypedef, MethodDefinition sourceMethod)
        {

            var targetModule = copyToTypedef.Module;

            // create a new MethodDefinition; all the content of sourceMethod will be copied to this new MethodDefinition

            var targetMethod = new MethodDefinition(sourceMethod.Name, sourceMethod.Attributes, targetModule.ImportReference(sourceMethod.ReturnType));


            // Copy the parameters; 
            foreach (var p in sourceMethod.Parameters)
            {
                var nP = new ParameterDefinition(p.Name, p.Attributes, targetModule.ImportReference(p.ParameterType));
                targetMethod.Parameters.Add(nP);
            }

            // copy the body
            var nBody = targetMethod.Body;
            var oldBody = sourceMethod.Body;

            nBody.InitLocals = oldBody.InitLocals;

            // copy the local variable definition
            foreach (var v in oldBody.Variables)
            {
                var nv = new VariableDefinition(targetModule.ImportReference(v.VariableType));
                //v.Name, 
                nBody.Variables.Add(nv);
            }

            // copy the IL; we only need to take care of reference and method definitions
            var col = nBody.Instructions;
            foreach (var i in oldBody.Instructions)
            {
                var operand = i.Operand;
                if (operand == null)
                {
                    col.Add(Instruction.Create(i.OpCode));
                }

                // for any methodef that this method calls, we will copy it

                else if (operand is MethodDefinition dmethod)
                {
                    var newMethod = copyToTypedef.Methods.FirstOrDefault(f =>
                        f.Name == dmethod.Name && f.Parameters.Count == dmethod.Parameters.Count);
                    if (newMethod == null)
                    {
                        var addMethodMeta = AddMethod.Read(dmethod);
                        if (addMethodMeta != null)
                        {
                            var targetType = targetMethod.Module.GetType(addMethodMeta.targetType);
                            newMethod = CopyMethod(targetType, dmethod);
                        }
                    }
                    col.Add(Instruction.Create(i.OpCode, newMethod));
                }

                // for member reference, import it
                else if (operand is FieldReference fref)
                {
                    FieldReference newf;
                    if (fref.DeclaringType == sourceMethod.DeclaringType)
                    {
                        var addFieldMeta = OvercookedControlsPatcher.AddField.Read(fref.Resolve());
                        var targetType = targetMethod.Module.GetType(addFieldMeta.targetType);
                        newf = targetType.Fields.First(f => f.Name == fref.Name);
                    }
                    else
                    {
                        newf = targetModule.ImportReference(fref);
                    }
                    col.Add(Instruction.Create(i.OpCode, newf));
                }
                else if (operand is TypeReference tref)
                {
                    var newf = targetModule.ImportReference(tref);
                    col.Add(Instruction.Create(i.OpCode, newf));
                }
                else if (operand is TypeDefinition tdef)
                {
                    var newf = targetModule.ImportReference(tdef);
                    col.Add(Instruction.Create(i.OpCode, newf));
                }
                else if (operand is MethodReference mref)
                {
                    var newf = targetModule.ImportReference(mref);
                    col.Add(Instruction.Create(i.OpCode, newf));
                }
                else
                {
                    // we don't need to do any processing on the operand
                    col.Add(i);
                }
            }

            // copy the exception handler blocks
            foreach (var eh in oldBody.ExceptionHandlers)
            {
                var neh = new ExceptionHandler(eh.HandlerType);
                if (eh.CatchType != null)
                {
                    neh.CatchType = targetModule.ImportReference(eh.CatchType);
                }

                // we need to setup neh.Start and End; these are instructions; we need to locate it in the source by index
                if (eh.TryStart != null)
                {
                    var idx = oldBody.Instructions.IndexOf(eh.TryStart);
                    neh.TryStart = col[idx];
                }
                if (eh.TryEnd != null)
                {
                    var idx = oldBody.Instructions.IndexOf(eh.TryEnd);
                    neh.TryEnd = col[idx];
                }

                nBody.ExceptionHandlers.Add(neh);
            }

            // Add this method to the target typedef
            copyToTypedef.Methods.Add(targetMethod);
            targetMethod.DeclaringType = copyToTypedef;
            return targetMethod;
        }

        public static void ReplaceMethod(MethodDefinition targetMethod, MethodDefinition sourceMethod)
        {
            var targetModule = targetMethod.DeclaringType.Module;

            // Create second method with original code
            var realMethod = new MethodDefinition(targetMethod.Name + "_old", targetMethod.Attributes, targetMethod.ReturnType);
            {
                // Copy the parameters
                foreach (var p in targetMethod.Parameters)
                {
                    var nP = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                    realMethod.Parameters.Add(nP);
                }

                // Copy method contents
                var nBody = realMethod.Body;
                var oldBody = targetMethod.Body;
                nBody.InitLocals = oldBody.InitLocals;

                // copy the local variable definition
                foreach (var v in oldBody.Variables)
                {
                    var nv = new VariableDefinition(v.VariableType);
                    nBody.Variables.Add(nv);
                }

                // copy the IL
                var col = nBody.Instructions;
                foreach (var i in oldBody.Instructions)
                {
                    col.Add(i);
                }

                // copy the exception handler blocks
                foreach (var eh in oldBody.ExceptionHandlers)
                {
                    var neh = new ExceptionHandler(eh.HandlerType)
                    {
                        CatchType = eh.CatchType
                    };

                    // we need to setup neh.Start and End; these are instructions; we need to locate it in the source by index
                    if (eh.TryStart != null)
                    {
                        var idx = oldBody.Instructions.IndexOf(eh.TryStart);
                        neh.TryStart = col[idx];
                    }
                    if (eh.TryEnd != null)
                    {
                        var idx = oldBody.Instructions.IndexOf(eh.TryEnd);
                        neh.TryEnd = col[idx];
                    }

                    nBody.ExceptionHandlers.Add(neh);
                }

                targetMethod.DeclaringType.Methods.Add(realMethod);
                realMethod.DeclaringType = targetMethod.DeclaringType;
            }

            // Replace original methods IL with proxy code
            {
                // copy the body
                var nBody = targetMethod.Body;
                var oldBody = sourceMethod.Body;

                nBody.InitLocals = oldBody.InitLocals;

                // copy the local variable definition
                nBody.Variables.Clear();
                foreach (var v in oldBody.Variables)
                {
                    var nv = new VariableDefinition(targetModule.ImportReference(v.VariableType));
                    nBody.Variables.Add(nv);
                }

                // copy the IL; we only need to take care of reference and method definitions
                var col = nBody.Instructions;
                col.Clear();
                foreach (var i in oldBody.Instructions)
                {
                    var operand = i.Operand;
                    if (operand == null)
                    {
                        col.Add(Instruction.Create(i.OpCode));
                    }

                    // for any methoddef that this method calls, we will copy it
                    else if (operand is MethodDefinition dmethod)
                    {
                        var newMethod = targetMethod.DeclaringType.Methods.FirstOrDefault(f =>
                            f.Name == dmethod.Name && f.Parameters.Count == dmethod.Parameters.Count);
                        if (newMethod == null)
                        {
                            var addMethodMeta = AddMethod.Read(dmethod);
                            if (addMethodMeta != null)
                            {
                                var targetType = targetMethod.Module.GetType(addMethodMeta.targetType);
                                newMethod = CopyMethod(targetType, dmethod);
                            }
                        }
                        col.Add(Instruction.Create(i.OpCode, newMethod));
                    }

                    // for member reference, import it
                    else if (operand is FieldReference fref)
                    {
                        FieldReference newf;
                        if (fref.DeclaringType == sourceMethod.DeclaringType)
                        {
                            var addFieldMeta = OvercookedControlsPatcher.AddField.Read(fref.Resolve());
                            var targetType = targetMethod.Module.GetType(addFieldMeta.targetType);
                            newf = targetType.Fields.First(f => f.Name == fref.Name);
                        }
                        else
                        {
                            newf = targetModule.ImportReference(fref);
                        }
                        col.Add(Instruction.Create(i.OpCode, newf));
                    }
                    else if (operand is TypeReference tref)
                    {
                        var newf = targetModule.ImportReference(tref);
                        col.Add(Instruction.Create(i.OpCode, newf));
                    }
                    else if (operand is TypeDefinition tdef)
                    {
                        var newf = targetModule.ImportReference(tdef);
                        col.Add(Instruction.Create(i.OpCode, newf));
                    }
                    else if (operand is MethodReference mref)
                    {
                        MethodReference newf = null;

                        /*MethodDefinition mdef = mref.Resolve();
                        if (mdef == sourceMethod)
                        {
                            newf = targetModule.ImportReference(targetMethod);
                        }
                        else */
                        if (mref.FullName.Equals(targetMethod.FullName))
                        //else if (mdef == targetMethod)
                        {
                            newf = targetModule.ImportReference(realMethod);
                        }
                        else
                        {
                            newf = targetModule.ImportReference(mref);
                        }
                        col.Add(Instruction.Create(i.OpCode, newf));
                    }
                    else
                    {
                        // we don't need to do any processing on the operand
                        col.Add(i);
                    }
                }

                // copy the exception handler blocks
                nBody.ExceptionHandlers.Clear();
                foreach (var eh in oldBody.ExceptionHandlers)
                {
                    var neh = new ExceptionHandler(eh.HandlerType);
                    if (eh.CatchType != null)
                    {
                        neh.CatchType = targetModule.ImportReference(eh.CatchType);
                    }

                    // we need to setup neh.Start and End; these are instructions; we need to locate it in the source by index
                    if (eh.TryStart != null)
                    {
                        var idx = oldBody.Instructions.IndexOf(eh.TryStart);
                        neh.TryStart = col[idx];
                    }
                    if (eh.TryEnd != null)
                    {
                        var idx = oldBody.Instructions.IndexOf(eh.TryEnd);
                        neh.TryEnd = col[idx];
                    }

                    nBody.ExceptionHandlers.Add(neh);
                }
            }
        }

        public static void AddField(TypeDefinition targetType, FieldDefinition patchField)
        {
            var targetModule = targetType.Module;
            targetType.Fields.Add(new FieldDefinition(patchField.Name, patchField.Attributes, targetModule.ImportReference(patchField.FieldType)));
        }
    }
}
