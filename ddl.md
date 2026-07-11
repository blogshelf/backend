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

UserID 采用加密二进制格式，最大 1KB，包含注册元数据，用于防伪造：

```text
[CreatedTimestamp : 8 bytes][ClientIP : 4 bytes][Random : N bytes][Padding]

使用环境变量中的公钥加密，博客所有者可自行解密验证。
攻击者无法构造有效 UserID，
```

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

| Name      | Type    |
|-----------|---------|
| UserID    | Binary  |
| State     | Integer |
| CreatedAt | Integer |
| UpdatedAt | Integer |

## Dynamic Authentication Columns

每一种认证方式对应一个动态 Binary 属性列。

例如：

```
srp
```

```
github
```

```
google
```

```
passkey
```

全部存储：

```
Binary(MsgPack)
```

程序根据当前登录方式读取对应列，无需反射，无需固定 Schema，未来新增认证方式无需修改数据库结构。

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
