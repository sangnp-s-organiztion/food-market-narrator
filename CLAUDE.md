# Food Market Narrator — AI Development Guide

## Project Overview

Food Market Narrator is a location-based mobile application that automatically narrates information about restaurants in Vinh Khanh Food Street.

When users walk near a restaurant, the app detects the location and automatically plays an audio narration describing the restaurant.

The system includes:

- Mobile App (Android)
- Backend API
- Seller/Admin Dashboard

---

# Tech Stack (.NET 10)
- Mobile UI: .NET Maui
- Backend: ASP.NET Core Web API (.NET 6+)
- Database: SQL Server
- ORM: Entity Framework Core

### Mobile

.NET MAUI (Android)

Responsibilities:

- GPS location tracking
- Geofence detection
- Trigger narration
- Play audio narration

Location:
FoodMarketNarrator.Maui

---

### Backend API

ASP.NET Web API

Responsibilities:

- Restaurant management
- Narration content
- Location data
- User authentication
- Order and restaurant management

Location:
FoodMarketNarrator.Api
---

### Web Dashboard

React + TypeScript + Vite

Used by:

- Sellers
- Admins

Responsibilities:

- Manage restaurants
- Upload narration content
- Manage menu
- Manage orders

Location:
saler/ for saler UI
admin/ for admin UI

---

## System Architecture

Client → API → Database

Mobile App
↓
ASP.NET Web API
↓
SQL Server

Web Dashboard
↓
ASP.NET Web API

## Architecture

Layered Architecture:
- Controller: def api for UI call
- Service: write logic of feature, it is seperate with controller to help pj is cleaner
- Repository: interact with Database
- Model: def table in Database

# Database Rules
- Use existing schema (do not invent new tables unless asked)
- Use parameterized queries (avoid SQL injection)
- Keep queries efficient

# Coding Rules
- Keep code simple and readable (YAGNI)
- Avoid duplication (DRY)
- Use meaningful naming
- Do not over-engineer

# Important Notes
- The goal is a smooth user experience for discovering food via audio narration
- Prioritize clarity, performance, and maintainability

# Behavior Rules
- Do not start coding immediately
- Always present a plan first
- Only modify necessary parts of the code
- Do not rewrite entire files unless required
- Ask before making major architectural changes

# Mobile Considerations
- Optimize performance for mobile devices
- Minimize API latency

# Data Transfer Rules
- Do NOT return Entity models directly
- Use DTOs for all API responses
- Use mapping (manual or AutoMapper)

# API Rules
- Use RESTful conventions
- All responses must be JSON
- Standard response format:

{
  "success": true,
  "data": ...,
  "message": ""
}

- Use proper HTTP status codes (200, 400, 401, 404, 500)

# Authentication & Authorization
- Use Cookie authentication
- Protect endpoints based on roles (Admin, Seller)
- Public endpoint is used for visitor.
- Do not expose sensitive data
