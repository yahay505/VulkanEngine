//left here for reference



// using System.Reflection;
// using System.Reflection.Emit;
//
// namespace VulkanEngine.ECS_internals;
//
// class Reflect_emit_solution
// {
//     private static readonly AssemblyBuilder _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicMethodBuilderAssembly"), AssemblyBuilderAccess.Run);
//     private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule("DynamicModule");
//     public static unsafe void CollectçöçöççöççöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçöçööçöçöçöçöçöçöçççöçöçöçöçöçöçöçöççöçöçöçöçöçööçöççöçöçöçöçöçöçöçöçöçöçöçöçöçöViaReflection()
//     {
//
//         var nameWithoutCollision = $"ECS_Call_Proxy_{Guid.NewGuid():N}";
//         var typeBuilder = _moduleBuilder.DefineType(nameWithoutCollision, TypeAttributes.Public | TypeAttributes.Class);
//
//         //get all types in all loaded assemblies
//         var a = AppDomain.CurrentDomain.GetAssemblies().
//             Where(ass=>!ass.IsDynamic)
//             .SelectMany(ass => ass.GetTypes())
//             .Where(t => t.CustomAttributes.Any(z=>z.AttributeType==typeof(ECSScanAttribute)))
//             .SelectMany(t=>t.GetMethods())
//             .Where(m=>m.CustomAttributes.Any(z=>z.AttributeType==typeof(ECSJobAttribute)))
//             .Select(m=>(m,(ECSJobAttribute)(Attribute.GetCustomAttribute(m,typeof(ECSJobAttribute))!)));
//             
//             
//             ;
//         var nameList = new List<string>();
//         foreach (var (method,attr) in a)
//         {
//             var name = $"proxy_{method.Name}";
//             nameList.Add(name);
//             var proxymet = typeBuilder.DefineMethod(
//                 name,
//                 MethodAttributes.Public | MethodAttributes.Static,
//                 null,
//                 new[] {typeof(void*)}
//             );
//             {
//                 var il = proxymet.GetILGenerator();
//                 il.Emit(OpCodes.Ldarg_0);
//                 il.Emit(OpCodes.Tailcall);
//                 il.EmitCall(OpCodes.Call, method, null);
//                 il.Emit(OpCodes.Ret);
//             }
//         }
//         var t = typeBuilder.CreateType();
//         var fns = nameList.Select(z=>t.GetMethod(z)!.MethodHandle.GetFunctionPointer()).ToList();
//
//         var q = new ECSQuery<Transform_ref, MeshData>();
//
// #pragma warning disable CS8500
//         ((delegate* managed<void*, void>) fns[0])(&q);
// #pragma warning restore CS8500
//     }
//     
// }