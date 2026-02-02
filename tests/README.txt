THƯ MỤC: tests/

MỤC ĐÍCH:
- Chứa các test project
- Unit tests, integration tests
- Test coverage cho toàn bộ codebase

NỘI DUNG:
- UnitTests/ - Unit tests cho services, helpers
- IntegrationTests/ - Integration tests cho repositories, APIs
- E2ETests/ - End-to-end tests (nếu có)
- Fixtures/ - Test fixtures, test data

CÁCH CHẠY:
```
dotnet test
dotnet test --logger "console;verbosity=detailed"
dotnet test /p:CollectCoverage=true
```

VÍ DỤ:
- ProductServiceTests.cs - Test ProductService
- ProductRepositoryTests.cs - Test ProductRepository
- ApiEndpointTests.cs - Test API endpoints
- AuthenticationTests.cs - Test xác thực

GHI CHÚ:
- Viết test cho mọi logic quan trọng
- Target coverage ≥ 80%
- Test phải độc lập và có thể chạy bất kỳ lúc nào
- Sử dụng xUnit, NUnit, hoặc MSTest
