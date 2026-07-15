# BlogShelf OTS Schema Specification v1.0

## Design Principles

* OTS 负责业务状态存储
* 时间统一使用 Unix Timestamp（Milliseconds，UInt64）
* Session 通过 OTS 表级 TTL 兜底清理，ExpiresAt 做应用层精细过期控制
* 所有业务对象使用 State 字段表示状态
* 按访问模式建模，不追求关系数据库范式

---

# Table: Post

## Purpose

博客元数据。

## Primary Key

Partition Key

```text
PostID : STRING (ULID)
```

## Attributes

| Name         | Type                      | Description                            |
|--------------|---------------------------|----------------------------------------|
| MonthID      | Integer                   | YYYYMM，用于按月查询                          |
| Sha512       | Binary                    | Markdown SHA512                        |
| ObjectKey    | String                    | OSS Object Key                         |
| ObjectSize   | Integer                   | Markdown Size(Bytes)                   |
| Title        | String                    | 标题                                     |
| Summary      | String                    | 摘要                                     |
| Tags         | Binary(MsgPack<string[]>) | 标签                                     |
| IsPrivate    | Boolean                   | 私有文章                                   |
| State        | Integer                   | Draft / Published / Archived / Deleted |
| AccessPolicy | Binary(MsgPack)           | ACL                                    |
| CreatedAt    | Integer                   | 创建时间                                   |
| UpdatedAt    | Integer                   | 修改时间                                   |
| Version      | Integer                   | 乐观锁/缓存版本                               |

## Secondary Indexes

### PostByDate

按创建时间倒序查询文章（首页时间线）。

```text
Index Name:   PostByDate
Primary Key:  CreatedAt (INTEGER) DESC
Covered:      Title, Summary, State, MonthID
Type:         Global
```

---

# Table: Comment

## Purpose

博客评论。

## Primary Key

Partition Key

```text
PostID : STRING (ULID)
```

Primary Key

```text
CommentID : STRING (ULID)
```

## Attributes

| Name            | Type                          |
|-----------------|-------------------------------|
| ParentCommentID | String (Nullable)             |
| UserID          | Binary (Size: 8192 Bits)      |
| Body            | String (Markdown Safe Subset) |
| State           | Integer                       |
| CreatedAt       | Integer                       |
| UpdatedAt       | Integer                       |
| UserSignature   | Binary                        |
| ClientKeyID     | String                        |

## Secondary Indexes

### CommentByUser

按用户查询评论列表（按时间倒序）。

```text
Index Name:   CommentByUser
Primary Key:  UserID (BINARY) + CreatedAt (INTEGER) DESC
Covered:      Body, State
Type:         Global
```

---

# Table: CommentLike

## Purpose

评论点赞关系。

## Primary Key

Partition Key

```text
CommentID : STRING (ULID)
```

Primary Key

```text
UserID : BINARY
```

无 Attribute。

---

# Table: User

## Purpose

用户公开资料。

## Primary Key

Partition Key

```text
PermissionCode : INTEGER
```

Primary Key

```text
UserID : BINARY
```

## UserID Binary Format

UserID 采用二进制格式，固定 8192 字节（8KB），使用 ECIES 加密（ECDH 密钥协商 + AES-GCM），博客所有者可用私钥本地解密验证：

```text
[Ephemeral PubKey : 65 bytes][Nonce : 12 bytes][Ciphertext : 8099 bytes][Auth Tag : 16 bytes]
```

- Ephemeral PubKey：临时 ECDH P-256 公钥（未压缩格式），用于派生共享密钥
- Nonce：AES-GCM 随机数（12 字节）
- Ciphertext：加密后的明文（8099 字节），明文结构为 `[CreatedTimestamp:8][ClientIP:16][Random:8075]`
- Auth Tag：AES-GCM 认证标签（16 字节）

密钥来源：`OWNER_PUBLIC_KEY` 环境变量（ECDSA P-256 PEM 格式，与 ECDH 可互换使用）。

## Attributes

| Name           | Type    |
|----------------|---------|
| Name           | String  |
| AvatarUrl      | String  |
| Bio            | String  |
| PermissionCode | Integer |
| State          | Integer |
| CreatedAt      | Integer |
| UpdatedAt      | Integer |

---

# Table: UserIdentity

## Purpose

认证信息。

## Primary Key

Partition Key

```text
IdentityType
```

例如：

```
srp
github
google
passkey
```

Primary Key

```text
IdentityKey
```

例如：

```
用户名

GitHub Subject

Google Subject

CredentialID
```

## Fixed Attributes

| Name      | Type            |
|-----------|-----------------|
| UserID    | Binary          |
| AuthData  | Binary(MsgPack) |
| State     | Integer         |
| CreatedAt | Integer         |
| UpdatedAt | Integer         |

## AuthData Column

固定列名 `AuthData`，类型 Binary(MsgPack)。存储该认证方式的具体数据，结构由 IdentityType 决定：

- `srp`：{verifier, salt, iterations}
- `github`：{subject, ...}
- `google`：{subject, ...}
- `passkey`：{credentialId, publicKey, ...}

每行只存自己那个 IdentityType 对应的数据，不再使用动态列名（列名与 IdentityType 重复，冗余）。新增认证方式只需新增 IdentityType 取值，无需修改表结构。

## Secondary Indexes

### UserIdentityByUserID

按 UserID 反查所有绑定身份。

```text
Index Name:   UserIdentityByUserID
Primary Key:  UserID (BINARY)
Covered:      State, CreatedAt, UpdatedAt
Type:         Global
```

注：IdentityType 和 IdentityKey 是数据表主键，会自动包含在索引结果中，无需重复指定为覆盖列。

---

# Table: Session

## Purpose

登录会话。

开启 OTS TTL（表级配置，所有行统一过期，适合做最大会话时长兜底清理）。
`ExpiresAt` 由应用层判断会话是否已过期，支持不同会话不同有效期。

## Primary Key

Partition Key

```text
UserID
```

Primary Key

```text
SessionID
```

## Attributes

| Name             | Type            |
|------------------|-----------------|
| RefreshTokenHash | Binary(SHA256)  |
| DeviceInfo       | Binary(MsgPack) |
| IPHash           | Binary          |
| UserAgent        | String          |
| CreatedAt        | Integer         |
| LastSeenAt       | Integer         |
| ExpiresAt        | Integer         |

## Secondary Indexes

> 注：Session 表开启了 TTL（2592000 秒），OTS 不支持在此类表上创建二级索引。
> 登录验证时按 SessionID 查询，需通过应用层维护的映射表或全表扫描实现。

---

# Table: Config

## Purpose

运行配置。

## Primary Key

Partition Key

```text
CONFIG
```

Primary Key

```text
CURRENT
```

## Fixed Attributes

| Name          | Type    |
|---------------|---------|
| SchemaVersion | Integer |
| ConfigVersion | Integer |
| OwnerUserID   | Binary  |

## Business Configuration

每一个配置项直接作为 Attribute。

例如：

```
Title
Subtitle
Theme
Language
Footer
Copyright
Notice
AllowComment
AllowRegister
HomepageLayout
...
```

新增配置项直接增加 Attribute，无需迁移 Schema。

---

# State Convention

统一采用 Integer。

推荐：

```
0 = Normal
1 = Disabled
2 = Deleted
```

实体可根据业务继续扩展。

---

# Time Convention

统一：

```
UInt64

Unix Timestamp

Milliseconds
```

禁止混用 DateTime、RFC3339、Unix Seconds。
