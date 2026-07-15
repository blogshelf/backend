using System.Text.Json.Serialization;

namespace backend.models;

[JsonSerializable(typeof(RegisterSrp))]
[JsonSerializable(typeof(VerifyForMail))]
[JsonSerializable(typeof(LoginSrp))]
public partial class AppJsonContext: JsonSerializerContext;