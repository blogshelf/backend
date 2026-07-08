# BlogShelf Storage Specification v1.0

## Design Principles

* OTS 负责业务状态存储
* OSS Vector Bucket 负责 Blob + 向量索引，统一使用一个桶
* 所有复杂对象统一采用 MsgPack(Binary)

---

# OSS Layout

```
posts/
attachments/
avatars/
pages/
themes/
```

推荐：

```
posts/{PostID}.md
```

正文全部放入 OSS。

数据库仅保存：

```
ObjectKey
```

---

# OSS Vector Index

## Purpose

语义搜索。对文章正文做 Embedding，写入 OSS 向量索引，实现近似文章推荐与关键词语义搜索。

## Index Schema

| 参数               | 值                                                 |
|------------------|---------------------------------------------------|
| Vector Dimension | 768 (text-embedding-3-small)                      |
| Data Type        | Float32                                           |
| Distance Metric  | Cosine                                            |
| Metadata         | PostID (String), Title (String), Summary (String) |

## Data Flow

```
发布文章 → Embedding API → OSS Vector Index (向量 + PostID)
                     ↘ OTS Post 表 (元数据)

用户搜索 → Embedding API → KnnQuery(Vector Index) → PostID[]
         → OTS BatchGetRow → 搜索结果
```

---

# Binary(MsgPack) Convention

统一采用 MsgPack 序列化：

* Tags
* AccessPolicy
* AuthenticationData
* DeviceInfo
* Extensions
* 未来所有复杂对象

---

# Storage Responsibility

## OSS Vector Bucket (唯一桶)

* Markdown
* 图片
* 附件
* 页面资源
* 文章向量索引（语义搜索、相似推荐）

## OTS

* User
* UserIdentity
* Session
* Post Metadata
* Comment
* CommentLike
* Config

---

# Architecture

```
                BlogShelf

          +------------------+
          |  ASP.NET Backend |
          +------------------+
              │          │
              │          ▼
              │   OSS Vector Bucket
              │   (文件 + 向量索引)
              │
              ▼
         Alibaba OTS

      用户/评论/配置
      元数据/Session
      身份认证
```
