# ⚡ FitHub Energy - Fitness Management System

**FitHub Energy** is a high-performance web application designed to manage fitness centers, categories, and training routines. Built with **ASP.NET Core MVC**, it features a modern, high-contrast "Dark Energy" interface with glassmorphism effects.

## 🚀 Current Progress

### 🎨 UI/UX Design
* **Vibrant Energy Theme:** Custom CSS with neon red accents and deep dark gradients.
* **Glassmorphism:** Semi-transparent cards and navigation bars using `backdrop-filter`.
* **Responsive Layout:** Fully adaptive design for mobile and desktop.
* **DataTables Integration:** Advanced tables with custom dark-mode styling and entry filtering.

  ## 📸 Screenshots

### 🏠 01. The Hero Dashboard
*High-impact visuals with a custom hero section to drive user engagement.*
><img width="2548" height="1351" alt="image" src="https://github.com/user-attachments/assets/77e28716-a8f2-44a0-bc9c-5152b23a8e80" />


### 📊 02. Categories Management (Admin View)
*Advanced DataTables integration with custom dark-mode filtering and pagination.*
> <img width="2536" height="1356" alt="image" src="https://github.com/user-attachments/assets/efb68c53-2a8e-4d4d-b295-487d6dd8f536" />


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
<img width="388" height="742" alt="image" src="https://github.com/user-attachments/assets/bba408c1-361f-4e8a-a957-d5f4a3f22c1a" />


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
