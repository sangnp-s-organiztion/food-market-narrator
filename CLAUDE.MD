# Food Market Narrator — AI Development Guide

## Project Overview

Food Market Narrator is a location-based mobile application that automatically narrates information about restaurants in Vinh Khanh Food Street.

When users walk near a restaurant, the app detects the location and automatically plays an audio narration describing the restaurant.

The system includes:

- Mobile App (Android)
- Backend API
- Seller/Admin Dashboard

---

## Tech Stack

### Mobile

.NET MAUI (Android)

Responsibilities:

- GPS location tracking
- Geofence detection
- Trigger narration
- Play audio narration

Location:
mobile/

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
backend/

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
saler/

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
