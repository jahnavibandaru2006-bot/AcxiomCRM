# AcxiomCRM

A full-stack Customer Relationship Management (CRM) application built with **ASP.NET Core**, **Entity Framework Core**, **SQLite**, and **ASP.NET Core Identity**.

## Features

- 🔐 Authentication and Role-Based Authorization
- 📊 Dashboard with CRM KPIs and Chart.js visualizations
- 👥 Customer Management
- 🎯 Lead Management and Lead Conversion
- 💼 Opportunity and Sales Pipeline Management
- 📅 Follow-Up Management
- 📝 Activity Management
- 👤 User and Role Management
- 🔎 Audit Logging and Security Events
- 📈 CRM Reports
- 🌐 REST APIs
- ✅ Server-side and client-side validation
- 🔒 Role-based data access

## User Roles

### Admin
- Full CRM access
- User and role management
- Audit log access
- Reports and dashboard

### Manager
- CRM management
- Pipeline and opportunity monitoring
- Follow-ups and activities
- Reports

### Sales Executive
- Access to assigned customers, leads, opportunities, follow-ups and activities

## Technology Stack

- **Backend:** ASP.NET Core 10
- **Language:** C#
- **ORM:** Entity Framework Core
- **Database:** SQLite
- **Authentication:** ASP.NET Core Identity
- **Authorization:** Role-Based Access Control (RBAC)
- **Frontend:** Razor Views, HTML, CSS, Bootstrap, JavaScript
- **Charts:** Chart.js
- **API:** ASP.NET Core REST APIs
- **Version Control:** Git and GitHub

## Project Structure

```text
AcxiomCRM/
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Services/
├── Views/
├── wwwroot/
├── Program.cs
├── SeedData.cs
├── appsettings.json
└── AcxiomCRM.csproj
POST /api/auth/login
POST /api/auth/logout

GET    /api/customers
POST   /api/customers
GET    /api/customers/{id}
PUT    /api/customers/{id}
DELETE /api/customers/{id}

GET  /api/leads
POST /api/leads

GET  /api/opportunities
POST /api/opportunities

GET  /api/followups
POST /api/followups

GET /api/reports/pipeline
