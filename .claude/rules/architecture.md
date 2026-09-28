---
paths:
  - "backend/**/*.cs"
  - "backend/**/*.csproj"
---

# Architecture Rules

## Dependency Direction

Dependencies must point inward.

- `Domain` depends on no other application project.
- `Application` depends on `Domain`.
- `Infrastructure` depends on `Application` and `Domain`.
- `Api` depends on `Application` and may reference `Infrastructure` only as required for application composition and dependency registration.
- Inner layers must never depend on outer layers.

Do not reference Infrastructure-specific types from Domain or Application.

## Domain

The Domain layer contains business concepts that are independent of
infrastructure and transport concerns.

Simple domain entities are the default.

- Keep Domain free of EF Core, ASP.NET Core, HTTP, persistence, and
  external-service dependencies.
- For CRUD-oriented applications and straightforward workflows, keep entities
  simple and enforce use-case/workflow rules in Application Services.
- Validation rules, immutable fields, status transitions, or allowed/forbidden
  edits do not by themselves justify a Rich Domain Model.
- Move behavior into Domain entities only when non-trivial domain invariants or
  domain behavior materially benefit from domain-level encapsulation
  independently of a particular application use case.
- Do not add factory methods, private setters, transition methods, Aggregate
  Roots, Domain Events, Specifications, Domain Services, systematic Value
  Objects, or similar DDD constructs merely to make a simple model appear more
  domain-driven.
- A Rich Domain Model or DDD tactical pattern requires explicit approval unless
  the approved project specification already requires that design.

## Application

The Application layer coordinates application use cases.

- Use Application Services as the default orchestration mechanism.
- Application Services may coordinate Domain objects, repositories, external-service abstractions, and the Unit of Work.
- Define persistence and external-service abstractions in Application when they are required by application use cases.
- Keep infrastructure implementation details out of Application.
- Do not use `DbContext`, `DbSet<T>`, EF Core APIs, provider-specific types, or ASP.NET Core transport types in Application.

Do not introduce CQRS handlers, MediatR, Kommand, Vertical Slice Architecture, or similar mediator-based organization unless explicitly approved.

A service becoming too large is a signal to reconsider its responsibilities, not an automatic reason to introduce another architectural pattern.

## API

ASP.NET Core Controllers are the default HTTP entry point.

Controllers must remain thin:

- bind and validate HTTP input;
- invoke Application services;
- translate application outcomes into HTTP responses.

Controllers must not:

- contain business logic;
- access `DbContext`;
- access concrete repositories;
- construct infrastructure services manually.

`Program.cs` is the application composition root and may register Infrastructure implementations.

## Repositories

Repository interfaces are inward-facing persistence contracts.

- Prefer specific repository interfaces that represent actual Application
  use-case persistence needs.
- Repository interfaces should represent cohesive persistence boundaries rather
  than blindly mirror every database table.
- When an explicitly approved DDD design contains Aggregate Roots, those
  Aggregate Roots are natural repository-boundary candidates.
- Define repository interfaces in Application.
- Implement repositories in Infrastructure.
- Keep LINQ-to-EF query construction inside Infrastructure.
- Do not expose `IQueryable<T>`, `DbSet<T>`, `DbContext`, EF Core expressions, or provider-specific types from repository interfaces.
- Return materialized entities, collections, projections, or explicit result models as appropriate.

Example:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken);
}
```

## Generic Repository

A generic repository may be used only as an Infrastructure implementation detail when it removes genuine duplication.

- Application code must not depend on `IGenericRepository<T>` or another generic CRUD contract.
- Concrete repositories may reuse an Infrastructure-only `Repository<TEntity>` base class.
- That base class may expose useful protected persistence helpers to concrete
  repositories.
- Do not expand the generic repository into a replacement for EF Core query capabilities.
- Use-case-specific queries belong in concrete repository implementations.

## Unit of Work

Use `IUnitOfWork` as the normal commit boundary for an Application use case.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

- Infrastructure implements `IUnitOfWork` using the application's `DbContext`.
- Repositories normally stage changes and do not independently call `SaveChangesAsync`.
- Application Services decide when an application operation is ready to commit.
- Repositories participating in the same use case must share the same Unit of Work.
- Repositories are injected explicitly into their consumers.
- `IUnitOfWork` must not act as a repository container or service locator that
  exposes all repositories as properties.
- Repositories participating in the same Unit of Work normally share the same
  scoped `DbContext`.
- Do not introduce explicit transaction APIs until a real use case requires transaction control beyond normal `SaveChangesAsync`.

## EF Core Boundary

EF Core is an Infrastructure implementation detail.

Inside Infrastructure, use EF Core normally and take advantage of its capabilities.

- Do not leak EF Core concepts into Application.
- Do not cripple repository implementations merely to preserve a generic abstraction.
- The repository boundary keeps persistence concerns out of Application; it does not hide EF Core from Infrastructure.

## Consequential Decision Gate

Ask for explicit approval before introducing a consequential design decision
that is not already required by the specification or established project
structure.

Examples include:

- Rich Domain Model;
- Aggregate Roots or systematic DDD tactical patterns;
- CQRS;
- MediatR;
- Vertical Slice Architecture;
- event sourcing;
- a project-wide Result/railway-oriented programming model;
- new architectural layers or production projects;
- multiple `DbContext` / persistence boundaries;
- substantially different persistence technology;
- messaging infrastructure;
- background-processing infrastructure;
- distributed caching;
- microservices;
- new authentication/authorization architecture;
- replacing an approved baseline framework/library with another technology.

Do not ask for approval for routine local implementation decisions already
constrained by the specification, approved stack, existing code, or these rules.

## Architectural Simplicity

Prefer the simplest architecture that satisfies current requirements.

Do not introduce additional architectural patterns or layers without a concrete need.

Prefer solving observed design problems over anticipating hypothetical future complexity.

Architectural complexity must be proportional to the problem being solved.

When a consequential architectural choice is not already required by the
specification or established project structure, ask for explicit approval before
introducing it.