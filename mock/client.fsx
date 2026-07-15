#r "bin/Debug/net11.0/backend.dll"
#r "nuget: MessagePack 3.1.7"
#r "nuget: MessagePack.FSharpExtensions 4.0.0"

open System
open System.Net.Http
open System.Text
open MessagePack
open MessagePack.FSharp
open backend.models

// ==================== 3. 配置 MessagePack 序列化器 ====================
// 为了确保 F# 和 C# 类型都能正确序列化，配置一个组合的解析器
let resolver = 
    Resolvers.CompositeResolver.Create(
        FSharpResolver.Instance,          // 处理 F# 的 discriminated unions, records, lists 等
        Resolvers.StandardResolver.Instance // 处理标准 .NET 类型
    )
let options = MessagePackSerializerOptions.Standard.WithResolver(resolver)
