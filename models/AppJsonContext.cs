using System.Text.Json.Serialization;

namespace backend.models;

[JsonSerializable(typeof(RegisterSrp))]
public partial class AppJsonContext: JsonSerializerContext;