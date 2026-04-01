# Test Strategy

## Mục tiêu

- Đảm bảo API contract ổn định cho MAUI/Seller/Admin.
- Giảm regression ở luồng narration + geofence.
- Giữ chất lượng release với smoke test trước deploy.

## Tầng test

- Unit test: logic đơn lẻ.
- Integration test: Controller -> Service -> Repository -> DB.
- Manual E2E: MAUI navigation, geofence, audio playback.

## Tài liệu test case hiện có

- `testing/unit/maui-unit-test-cases.md`
- `testing/integration/api-integration-test-cases.md`
