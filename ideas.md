# BlogShelf Ideas & Inspirations

---

## FHE Anti-Bot Challenge

### 概述

使用全同态加密（FHE）作为反 bot 挑战机制，用于在触发 rate limit 后验证客户端身份。

### 核心思路

服务器加密谜题 → 客户端 FHE 计算（不解密） → 返回加密结果 → 服务器解密验证

### 协议流程

1. 客户端触发 rate limit
2. 服务端返回专用挑战页面（与正常客户端分离）
3. 挑战页面加载 WASM FHE 库，请求加密谜题
4. 服务端下发 `{ ciphertext, challenge_nonce, session_id }`
5. 客户端 WASM 执行 `result = fhe_compute(ciphertext, nonce)`
6. 客户端提交加密结果
7. 服务端私钥解密验证

### 配置

```
FHEChallengeEnabled : Boolean  (默认 false，仅作者本人博客开启)
FHEChallengeLevel   : Integer  (0=关闭, 1=轻量, 2=重型)
```

### 谜题复杂度

算法自动匹配，根据客户端行为动态调整计算量。

### Session 绑定

挑战与 session 绑定，防止重放攻击。

### 失败策略

- 允许重试 10 次
- 全部失败 → 直接 ban

### 技术选型

- **FHE 方案**：Microsoft SEAL（BFV/CKKS）
- **服务端**：Microsoft.Research.SEALNet（NuGet 包）
- **客户端**：.NET for WebAssembly（同 SEALNet，前后端 API 一致，密钥格式兼容，无需额外 WASM 体积）

### 优势

- 服务器开销极低（一次加密 + 一次解密）
- 爬虫端开销极高（FHE 计算不可通过 ASIC 加速）
- 对合法用户影响可控（500ms 内）

### 已知弱点

- 爬虫可用原生 C++ FHE 库绕过 WASM 瓶颈
- 可并行化（多机分布式爬取稀释单机开销）
- FHE 库可预加载复用

### 定位

默认关闭的重磅手段，仅在作者本人博客上开启。不激进的爬虫放行。

---

## Auth Token 重构：SeedSession + 加密时间戳令牌

### 目标

消除 seed 网络传输，实现 proof-of-possession 令牌，建立可扩展身份源架构。

### 核心原则

1. **SeedSession 从 K 本地派生**：`HKDF(K, "seed-session")`，不走网络
2. **proof-of-possession**：客户端用密钥加密时间戳，服务端解密验证
3. **MessagePack 二进制**：无 base64 膨胀（仅 HTTP header 传输层用 base64url）
4. **TTL 自动清理**：Session 表靠 OTS 表级 TTL，不需要单独的 refresh token 过期时间
5. **分钟级时间戳**：token 有效窗口 ±1 分钟，天然防重放

### 密钥派生链

```
SRP 完成 → 双方各自独立计算 K
         ↓
SeedSession = HKDF(K, "seed-session")     ← 两边本地算，不走网络
    ├── requestKey = HKDF(SeedSession, "request-key")   ← 生成/验证 access token
    └── refreshKey = HKDF(SeedSession, "refresh-key")   ← 验证 refresh token + 轮换
```

- K：SRP 产物，登录后双方都可计算，计算完丢弃
- SeedSession：长期根密钥，存客户端本地 + Session 表
- requestKey：每次请求用，加密时间戳
- refreshKey：刷新时用，加密时间戳 + 服务端用它加密新 SeedSession 发回客户端

### 登录流程

#### SRP Begin（不变）

`handler/auth.cs:94-170` — `LoginBegin`

- 客户端发 `{username, A}`
- 服务端生成 `{B, ServerSecret}`，存 Session（`SrpState=false`）
- 返回 `{salt, B, token}`（token 是 sessionId）

#### SRP Complete（改造 `methods.cs:94-180`）

**现在的代码（要删掉）：**

```csharp
// line 160-161: 随机 seed + 加密传输（删掉）
var seed = RandomNumberGenerator.GetBytes(32);
var encryptedSeed = AesGcmHelper.Encrypt(K, seed);

// line 163-169: 传统 refresh token（删掉）
var refreshKey = HKDF.DeriveKey(..., seed, ...);
var refreshToken = AesGcmHelper.Encrypt(refreshKey, ...);

// line 171: 存 seed（改）
session.UpdateToAuthenticated(client, Hash(refreshToken), seed);
```

**改成：**

```csharp
var seedSession = HKDF.DeriveKey(
    HashAlgorithmName.SHA256, K, 32,
    info: "seed-session"u8.ToArray());

session.UpdateToAuthenticated(client, seedSession);
session.CleanUpSrpData(client);

return new LoginCompleteReturn
{
    Proofed = true,
    ServerProof = ComputeServerProof(...)
    // 不再返回 EncryptedSeed
};
```

客户端从 K 自己算 `SeedSession = HKDF(K, "seed-session")`，不需要服务端发送。

### 请求认证（Access Token）

#### 客户端生成

```javascript
// 每次请求前
timestamp = Math.floor(Date.now() / 1000 / 60)   // 分钟精度
salt = crypto.getRandomValues(new Uint8Array(16))
payload = MsgPack.encode({t: timestamp, s: salt})
token = AES_GCM(requestKey, payload)
// requestKey = HKDF(seedSession, "request-key")
```

#### Wire Format

```
Authorization: Bearer <base64url(MsgPack{u: userId, s: sessionId, p: token_bytes})>
```

`u`、`s` 是明文（用于 DB 查找），`p` 是加密的 proof。

#### 服务端验证

1. 解析 Header → `MsgPack{u, s, p}`
2. `Session(u, s)` → 取 `SeedSession`（从 Session 表）
3. `requestKey = HKDF(SeedSession, "request-key")`
4. `AesGcmHelper.Decrypt(requestKey, p)` → 得到 `{t, s}`
5. `|t - server_now_minutes| ≤ 1` → 通过

#### Token 大小

```
AES-GCM: 12(nonce) + 16(tag) + ~24(plaintext) = ~52 字节
加上 MsgPack envelope (u + s + p): ~130 字节
base64url 传输: ~180 字节
```

### 刷新流程（密钥轮换）

```
客户端 → 服务端:
  refreshKey = HKDF(seedSession, "refresh-key")
  refreshToken = AES_GCM(refreshKey, MsgPack{t: minutes, s: random(16)})

服务端:
  用 refreshKey 解密 refreshToken → 验证时间戳
  seedSessionNew = HKDF(seedSession, "rotate")
  存 seedSessionNew 到 Session 表
  响应: AES_GCM(refreshKey, seedSessionNew)

客户端:
  用旧 refreshKey 解密响应 → seedSessionNew
  覆盖旧 seedSession
  旧 seedSession 立即失效
```

刷新后旧密钥链全部失效。如果攻击者偷了 refresh token，只能在 ±1 分钟窗口内用一次，
且轮换后客户端的 seedSession 也变了。

### Session 表改造（`schema.cs`）

#### 字段变更

| 字段                                               | 操作      | 说明                                           |
|--------------------------------------------------|---------|----------------------------------------------|
| `RefreshTokenHash`                               | **删除**  | 不再需要，验证逻辑改用 SeedSession + 时间戳                |
| `Seed`                                           | **重命名** | → `SeedSession`，存储 `HKDF(K, "seed-session")` |
| `ExpiresAt`                                      | 保留      | OTS 表 TTL 自动清理                               |
| `SrpState` / `SrpA` / `SrpB` / `SrpServerSecret` | 不变      | SRP 临时数据，登录后 CleanUp 删除                      |

#### `UpdateToAuthenticated` 签名变更

**现在（`schema.cs:675`）：**

```csharp
public void UpdateToAuthenticated(OTSClient client, byte[] refreshTokenHash, byte[] seed)
```

**改成：**

```csharp
public void UpdateToAuthenticated(OTSClient client, byte[] seedSession)
```

内部 PUT 列：`SrpState=true`, `SeedSession=seedSession`, `LastSeenAt=now`, `ExpiresAt=now+30d`

#### `GetSingle` 读取变更

`Seed` → `SeedSession`（case 分支重命名）

### LoginCompleteReturn 改造（`Srp.cs:132-141`）

**现在：**

```csharp
public sealed record LoginCompleteReturn
{
    [Key("proofed")]    public required bool Proofed { get; init; }
    [Key("server_proof")] public required byte[]? ServerProof { get; init; }
    [Key("encrypted_seed")] public required byte[]? EncryptedSeed { get; init; }
}
```

**改成：**

```csharp
public sealed record LoginCompleteReturn
{
    [Key("proofed")]      public required bool Proofed { get; init; }
    [Key("server_proof")] public required byte[]? ServerProof { get; init; }
    // EncryptedSeed 删除 — 客户端本地从 K 派生 SeedSession
}
```

### 新建文件

#### `backend/auth/TokenService.cs`

```csharp
using System.Security.Cryptography;

namespace backend.auth;

public static class TokenService
{
    // === 密钥派生 ===
    public static byte[] DeriveSeedSession(byte[] k) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, k, 32,
            info: "seed-session"u8.ToArray());

    public static byte[] DeriveRequestKey(byte[] seedSession) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, seedSession, 32,
            info: "request-key"u8.ToArray());

    public static byte[] DeriveRefreshKey(byte[] seedSession) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, seedSession, 32,
            info: "refresh-key"u8.ToArray());

    public static byte[] RotateSeedSession(byte[] seedSession) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, seedSession, 32,
            info: "rotate"u8.ToArray());

    // === Access Token ===
    // 生成: AES-GCM(requestKey, MsgPack{t: minutes, s: salt})
    public static byte[] CreateAccessToken(byte[] requestKey, byte[] sessionId);
    // 验证: 解密 → 检查 |t - now| ≤ 1 → 返回 sessionId
    public static byte[]? VerifyAccessToken(byte[] requestKey, byte[] token);

    // === Refresh Token ===
    // 生成: AES-GCM(refreshKey, MsgPack{t: minutes, s: salt})
    public static byte[] CreateRefreshToken(byte[] refreshKey);
    // 验证: 解密 → 检查 |t - now| ≤ 1
    public static bool VerifyRefreshToken(byte[] refreshKey, byte[] token);
}
```

#### `backend/middleware/SessionAuthHandler.cs`

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace backend.middleware;

public class SessionAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. 从 Authorization header 取 "Bearer <base64url>"
        // 2. base64url decode → MsgPack deserialize → {u: userId, s: sessionId, p: token}
        // 3. 查 Session 表: new Session { UserId=u, SessionId=s }.GetSingle(client)
        // 4. tokenService.DeriveRequestKey(session.SeedSession)
        // 5. tokenService.VerifyAccessToken(requestKey, p)
        // 6. 成功 → new ClaimsPrincipal(new ClaimsIdentity(...)) → AuthenticateResult.Success
        //    失败 → AuthenticateResult.Fail
    }
}
```

#### `backend/auth/IIdentityLoginProvider.cs`

```csharp
namespace backend.auth;

public interface IIdentityLoginProvider
{
    string IdentityType { get; init; }
}
```

标记接口，未来 OAuth / Magic Link / Passkey 实现用。

### 修改文件清单

| # | 文件                            | 行号                         | 改动摘要                                                                                                                                                 |
|---|-------------------------------|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | `backend/Utils/Srp.cs`        | 132-141                    | `LoginCompleteReturn` 删除 `EncryptedSeed` 字段                                                                                                          |
| 2 | `backend/database/methods.cs` | 160-179                    | 删 `RandomSeed` + `EncryptedSeed` + `refreshKey` + `refreshToken`，改用 `HKDF(K, "seed-session")` + `session.UpdateToAuthenticated(client, seedSession)` |
| 3 | `backend/database/schema.cs`  | 571, 587, 668-669, 675-701 | 删 `RefreshTokenHash`，`Seed`→`SeedSession`，`UpdateToAuthenticated` 签名改，`GetSingle` case 改                                                             |
| 4 | `backend/handler/auth.cs`     | 217-221                    | `LoginCompete` 返回新 `LoginCompleteReturn`（无 `EncryptedSeed`）                                                                                          |
| 5 | `backend/models/response.cs`  | 85-90                      | `SrpAuthed` 删除 `RefreshToken` 字段（如果有）                                                                                                                |
| 6 | `backend/Program.cs`          | 32-33                      | 注册 auth scheme: `builder.Services.AddAuthentication("Session").AddScheme<...>("Session", ...)` + `app.UseAuthentication()` 在 `UseAuthorization` 之前   |

### 实现顺序

1. **`backend/auth/TokenService.cs`** — 纯函数，无依赖，先写好测试
2. **`backend/Utils/Srp.cs`** — `LoginCompleteReturn` 删 `EncryptedSeed`
3. **`backend/database/schema.cs`** — `Session` 表字段 + 方法改造
4. **`backend/database/methods.cs`** — `CompleteSrpReturn` 改造
5. **`backend/handler/auth.cs`** — `LoginCompete` 返回值适配
6. **`backend/middleware/SessionAuthHandler.cs`** — 认证中间件
7. **`backend/Program.cs`** — 注册 auth scheme + 中间件
8. **`backend/auth/IIdentityLoginProvider.cs`** — 标记接口（最简单）

### 安全特性总结

| 攻击             | 防御                                              |
|----------------|-------------------------------------------------|
| Token 窃取       | proof-of-possession：需要 SeedSession 才能生成有效 token |
| 重放攻击           | 分钟级时间戳，±1 分钟窗口自动过期                              |
| Token 伪造       | AES-GCM 认证加密，篡改密文解密失败                           |
| 中间人窃听          | SeedSession 不走网络；K 只在 SRP 交换时存在                 |
| 刷新 token 窃取    | 单次使用，轮换后旧密钥链失效                                  |
| SeedSession 泄露 | 可通过刷新轮换（`HKDF(seedSession, "rotate")`）          |
