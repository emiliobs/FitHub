# ⚡ FitHub Energy - Fitness Management System

**FitHub Energy** is a high-performance web application designed to manage fitness centers, categories, and training routines. Built with **ASP.NET Core MVC**, it features a modern, high-contrast "Dark Energy" interface with glassmorphism effects.

## 🚀 Current Progress

### 🎨 UI/UX Design
* **Vibrant Energy Theme:** Custom CSS with neon red accents and deep dark gradients.
* **Glassmorphism:** Semi-transparent cards and navigation bars using `backdrop-filter`.
* **Responsive Layout:** Fully adaptive design for mobile and desktop.
* **DataTables Integration:** Advanced tables with custom dark-mode styling and entry filtering.

### 🔐 Security & Identity
* **ASP.NET Core Identity:** Fully integrated for user management.
* **Role-Based Access Control (RBAC):** Roles defined for **Admin, Trainer, and Member**.
* **DbInitializer:** Automated seeding of roles and an initial Admin user.
* **Authentication Flow:** `AccountController` implemented for Login, Register, and Logout.

### 📋 Category Management (CRUD)
* Full Create, Read, Update, and Delete functionality.
* **Server-side Validation:** Duplicate name detection and data integrity checks.
* **User Feedback:** Integration with **SweetAlert2** for success and error notifications.

## 📸 Screenshots
*(Coming soon - Uploading to /screenshots folder)*

---

## 🛠️ Tech Stack
* **Framework:** .NET 10.0 (ASP.NET Core MVC)
* **IDE:** Visual Studio 2026
* **Database:** SQL Server (Entity Framework Core)
* **Frontend:** Bootstrap 5, jQuery, DataTables, SweetAlert2, Fetch API.

## ⚙️ Setup & Installation
1.  **Clone** the repository.
2.  **Update** the `DefaultConnection` in `appsettings.json`.
3.  **Run** `Update-Database` in the Package Manager Console.
4.  **Launch** the application (The `DbInitializer` will automatically create the Admin user).
