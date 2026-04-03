# Architecture Overview

## Hệ thống

Food Market Narrator gồm 4 thành phần chính:

- `FoodMarketNarrator.Maui`: mobile app cho visitor.
- `FoodMarketNarrator.Api`: backend REST API.
- `saler`: web app cho seller.
- `admin`: portal quản trị.

## Luồng tổng quan

Client (MAUI / Saler / Admin) -> API -> SQL Server (+ MongoDB cho dữ liệu bổ sung) -> static media files.

## Layer backend

Controller -> Service -> Repository -> DbContext.

Nguyên tắc:

- Controller mỏng, xử lý request/response.
- Business logic ở Service.
- Data access ở Repository.

## Tài liệu liên quan

- `architecture/api-architecture.md`
- `api/mongodb-setup.md`
- `mobile/overview-current-features.md`
