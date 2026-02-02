# Contributing to Food Market Narrator

Thank you for your interest in contributing! Please follow these guidelines.

## 📋 Development Workflow

### 1. Branch Naming
- `feature/feature-name` - New features
- `bugfix/bug-name` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring

### 2. Commit Messages
Follow conventional commits:
```
type(scope): subject

- feat: New feature
- fix: Bug fix
- docs: Documentation
- refactor: Code refactoring
- test: Adding tests
- style: Code style changes
```

### 3. Pull Requests
1. Create a feature branch
2. Make your changes
3. Write/update tests
4. Submit a PR with description
5. Ensure CI/CD passes

## 🏗️ Architecture Guidelines

### Clean Architecture Layers
- **Presentation**: Controllers, Views (Visitor, Saler, Admin)
- **Application**: Business logic, DTOs, Service interfaces
- **Domain**: Core entities, domain rules, enums
- **Infrastructure**: Database, external services
- **Shared**: Utilities, constants, helpers

## 🧪 Testing
- Write unit tests for services
- Write integration tests for repositories
- Aim for >80% code coverage

## 📐 Code Standards
- Follow C# naming conventions (PascalCase for classes/methods)
- Use async/await for I/O operations
- Add XML documentation comments to public APIs
- Keep methods focused and small

## 🔍 Code Review
Your PR will be reviewed for:
- Code quality
- Test coverage
- Documentation
- Architecture alignment

## ❓ Questions?
Open an issue or start a discussion.
