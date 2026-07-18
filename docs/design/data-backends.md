# Design — Pluggable Memory & Knowledge backends

**Status:** draft · **Category:** DESIGN · **Nature:** Data (`Recall`)

## Purpose

Memory and Knowledge are the writable/readable halves of the **Data** nature. Today both are
file-backed. This design makes their *storage* pluggable — files, a local database, or a cache like
Redis — **without changing the interface and without adding a dependency to the core**. It mirrors
exactly what we did for the brain: the core owns the contract and a zero-dependency default; every
external resource is an **opt-in package you pull in**.

The guiding requirement: **full local, full offline by default; online when you opt in.**

## The seam stays exactly as it is

The two broker interfaces are the contract, and they do **not** change:

```csharp
public interface IMemoryBroker
{
    ValueTask<IReadOnlyList<string>> SelectMemoriesAsync();
    ValueTask InsertMemoryAsync(string memory);
}

public interface IKnowledgeBroker
{
    ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query);
}
```

The core ships the **offline defaults** (already present, zero dependencies):

- `MemoryBroker(path)` — one append-per-line text file.
- `KnowledgeBroker(path, pattern, maxResults)` — a folder searched on each turn.

A bare agent is therefore fully local and fully offline with no extra packages. This is the floor,
and it never regresses.

## The one idea that keeps the interface stable: identity is a *construction* concern

A shared store (Redis, Postgres) needs to know *whose* memory this is and *where* knowledge lives.
That partitioning is **baked into the concrete broker at construction**, not threaded through the
interface:

```csharp
// file  — identity is the path
.UseMemory(new MemoryBroker("agent-memory.txt"))

// redis — identity is the key
.UseMemory(new RedisMemoryBroker(connection, key: "agent:hassan"))

// postgres — identity is the table/partition
.UseMemory(new PostgresMemoryBroker(connectionString, scope: "hassan"))
```

One broker instance = one scope. Multi-tenant hosts construct one broker per user/session. The
interface — and therefore every service above it — is untouched. This is why "same interface,
different backend" actually holds.

## The adapter pattern (identical to the brain)

Each external resource is its own **package** (and repo), following
[`Standard.Agents.Decision.Brains.LlamaSharp`](https://github.com/hassanhabib/Standard.Agents.Decision.Brains.LlamaSharp):

- One class implementing `IMemoryBroker` or `IKnowledgeBroker`.
- Owns its dependency (`StackExchange.Redis`, `Npgsql`, `Microsoft.Data.Sqlite`, …) — the core stays
  clean.
- References the published `Standard.Agents` core, not a project reference.
- Plugs in through the existing `.UseMemory(...)` / `.UseKnowledge(...)`.
- Ships an optional fluent extension so it reads well: `agent.UseMemoryRedis(connection, "agent:hassan")`.

## Package taxonomy

Named for the tri-nature: these are the **Data** nature's stores.

| Package | Backs | Dependency | Offline |
|---|---|---|---|
| `Standard.Agents` (core) | `MemoryBroker`, `KnowledgeBroker` (files) | none | ✅ default |
| `Standard.Agents.Data.Memory.Sqlite` | `IMemoryBroker` | Microsoft.Data.Sqlite | ✅ local db |
| `Standard.Agents.Data.Memory.Redis` | `IMemoryBroker` | StackExchange.Redis | ⚠️ local or remote |
| `Standard.Agents.Data.Memory.Postgres` | `IMemoryBroker` | Npgsql | ⚠️ local or remote |
| `Standard.Agents.Data.Memory.MsSql` | `IMemoryBroker` | Microsoft.Data.SqlClient | ⚠️ local or remote |
| `Standard.Agents.Data.Memory.Oracle` | `IMemoryBroker` | Oracle.ManagedDataAccess.Core | ⚠️ local or remote |
| `Standard.Agents.Data.Knowledge.Sqlite` | `IKnowledgeBroker` | Microsoft.Data.Sqlite (FTS5) | ✅ local db |
| `Standard.Agents.Data.Knowledge.Redis` | `IKnowledgeBroker` | StackExchange.Redis | ⚠️ local or remote |
| `Standard.Agents.Data.Knowledge.Postgres` | `IKnowledgeBroker` | Npgsql | ⚠️ local or remote |
| `Standard.Agents.Data.Knowledge.MsSql` | `IKnowledgeBroker` | Microsoft.Data.SqlClient | ⚠️ local or remote |
| `Standard.Agents.Data.Knowledge.Oracle` | `IKnowledgeBroker` | Oracle.ManagedDataAccess.Core | ⚠️ local or remote |

"⚠️ local or remote" = the engine can run on the same machine (offline) or across the network
(online). Redis, Postgres, SQL Server and Oracle all have local modes, so **offline is never lost**
— it's a deployment choice, not a code one.

**The list is open-ended.** Nothing about the core knows or cares which stores exist — each is just
another broker with a client library. Oracle, MySQL, MongoDB, a blob/S3 store, or raw key-value —
if it can hold a list of strings (memory) or answer a text query (knowledge), it can be a package.
The core only ever ships the **file** default; it never takes a database dependency.

## Retrieval per backend — the interface hides it, each backend does its best

The interface passes a query; **how** a backend matches is its own business, and databases do it
*better than files for free*:

| Backend | Memory recall | Knowledge search |
|---|---|---|
| File (core) | read all lines | case-insensitive **substring** of the whole prompt (naive) |
| SQLite | rows by scope, newest first | **FTS5** full-text |
| Redis | list/stream by key | RediSearch (or key scan) |
| Postgres | rows by scope | `tsvector` full-text / `ILIKE` |
| MsSql | rows by scope | full-text / `LIKE` |

So moving knowledge from a folder to SQLite/Postgres is not just portability — it's a **retrieval
upgrade** (real tokenized full-text instead of full-prompt substring), with no interface change.

## Non-goals for v1 (deliberate)

- **No embeddings / vector / semantic search yet.** That would change the *result shape* (scored,
  chunked, citable) and pull in an embedding dependency — a real evolution of the Data contract, and
  therefore a **SPEC** conversation, not something to smuggle into an adapter. Full-text search
  covers a lot of ground first. When we do it, it's `KnowledgeResult { Text, Score, Source }` + an
  `IEmbeddingBroker`, designed on purpose.
- **No ingestion/indexing in the broker.** Brokers *read* (and memory appends). Bulk-loading a
  knowledge store (chunk → index) is an out-of-band concern — a small indexer/CLI later. The broker
  stays a clean read seam.

## Consistency rules (The Standard / SPAL)

- **One resource, one broker.** A Redis broker talks to Redis and nothing else.
- **Core stays dependency-free.** Every native/client dependency lives in an adapter, never core.
- **Externally mockable.** The file default *is* the airplane-mode fallback; tests never need a live
  server.
- Each adapter is itself a small subsystem and follows Brokers/Services/Exposers if it grows.

## Open decision — repo layout

Two viable shapes; both publish the same per-backend packages:

1. **One repo per package** (max isolation, matches `…LlamaSharp`). More repos to manage.
2. **One `Standard.Agents.Data` repo, many packages** (`Memory.Redis`, `Knowledge.Postgres`, …).
   Shared CI, shared test helpers, one place to evolve the Data family — while each still ships as
   its own NuGet package.

**Recommendation:** option 2 for the Data family — the adapters share an interface, test doubles, and
release cadence, so one repo keeps them coherent without sacrificing independent packages.

## Build order

1. This design.
2. Reference adapter per capability, offline-first: **`Data.Memory.Sqlite`** and
   **`Data.Knowledge.Sqlite`** (local db, fully offline) — they prove the pattern end to end.
3. **`Data.Memory.Redis`** + **`Data.Knowledge.Redis`** (the cache/online path).
4. Postgres / MsSql as demand shows up.

Each adapter: bottom-up, one class, its own tests against a local engine (or a mockable seam), its
own `0.1.0.0` package referencing the published core.
