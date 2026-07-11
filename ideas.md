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
