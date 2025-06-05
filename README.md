# Restaurant Review Application: Setup & Run Guide

This document provides a step-by-step guide to getting the ASP.NET Core Razor Pages restaurant review application up and running locally with a PostgreSQL backend. You will:

1. Install prerequisites
2. Create a PostgreSQL database and import the provided SQL dump (`SQLDumpFinal.sql`) via pgAdmin
3. Configure the application’s connection string in `appsettings.json`
4. Build and run the application
5. Verify that the site is working

---

## Table of Contents

1. [Prerequisites](#prerequisites)  
2. [Project Structure & Files](#project-structure--files)  
3. [Step 1: Install & Configure PostgreSQL](#step-1-install--configure-postgresql)  
4. [Step 2: Import `SQLDumpFinal.sql` via pgAdmin](#step-2-import-sqldumpfinalsql-via-pgadmin)  
5. [Step 3: Update Connection String in `appsettings.json`](#step-3-update-connection-string-in-appsettingsjson)  
6. [Step 4: Install .NET SDK & Restore Packages](#step-4-install-net-sdk--restore-packages)  
7. [Step 5: Build and Run the Application](#step-5-build-and-run-the-application)  
8. [Step 6: Verify the Application](#step-6-verify-the-application)  
9. [Troubleshooting & Tips](#troubleshooting--tips)  
10. [Appendix: Sample `appsettings.json`](#appendix-sample-appsettingsjson)  

---

## Prerequisites

Before you begin, make sure you have the following installed on your local machine:

- **Windows 10/11, macOS, or Linux** (this guide assumes you’re on Windows, but steps are similar on macOS/Linux)
- **.NET 7.0 SDK (or later)**  
  Verify installation by opening a terminal/PowerShell and running:
  ```bash
  dotnet --version
    ```
You should see a 7.x.x or higher version.
- **PostgreSQL 14 (or later)**
We will use pgAdmin 4 to import the SQL dump. If you don’t have PostgreSQL installed yet, download and install it from https://www.postgresql.org/download/. During the installer, make note of:
    - The PostgreSQL service port (default: 5432)
    - The superuser username (postgres by default)
    - The superuser password you set during installation
- **pgAdmin 4** (usually bundled with PostgreSQL installer) 
We will use the pgAdmin GUI to create a new database and run the SQL script.
- **Text Editor / IDE** (e.g., Visual Studio 2022, Visual Studio Code)

---

## Step 1: Install & Configure PostgreSQL
1. Download & Install PostgreSQL
    1. Go to https://www.postgresql.org/download/.
    2. Choose your operating system and follow the installer instructions.
    3. During installation, note:
        - Port: 5432 (default)
        - Superuser username: postgres
        - Superuser password: (your own choice)
    4. Let the installer install pgAdmin 4 as well (it’s included in most distributions on Windows).
2. Verify PostgreSQL Service
    - On Windows, open Services (services.msc), find postgresql-x64-14 (or whatever version you installed) and ensure it’s running.
    - On macOS/Linux, run:
    ```bash
    sudo service postgresql status
    ```
    You should see that the service is active/running.
3. Open pgAdmin 4
    - Launch pgAdmin 4 from your Start Menu (Windows) or Applications folder (macOS).
    - When prompted to set a new master password for pgAdmin, choose a password you’ll remember.
4. Connect to Your Local Server
    - In the left‐hand “Browser” pane, expand Servers > PostgreSQL <version>.
    - If you’re asked for a password, enter the superuser (postgres) password you created during installation.
    - Once connected, you’ll see a list of default databases (`postgres`, `template1`, etc.).

---

## Step 2: Import `SQLDumpFinal.sql` via pgAdmin

1. **Create a New Database**
    1. In pgAdmin’s left tree, right‐click on Databases and choose Create > Database….
    2. In the Create – Database dialog:
        - **Database name**: **RestaurantReviewsDB** (or any name you prefer—just be consistent later)
        - **Owner**: postgres (or another superuser/role you plan to use)
2. **Open the Query Tool**
    1. Right‐click on the newly created database (`RestaurantReviewsDB`) and choose Query Tool.
    2. A new query editor tab appears, connected to `RestaurantReviewsDB`.
3. **Load & Execute the SQL Dump**
    1. In the query editor, click the “Open File” icon (or press Ctrl+O) to browse for `SQLDumpFinal.sql`.
    2. Navigate to the folder where you placed `SQLDumpFinal.sql` (project root) and open it.
        - You should now see a long SQL script that contains `CREATE TABLE`, `INSERT`, etc.
    3. Click the **Execute/Run** button or press F5.
    4. Wait until the message at the bottom says **Query returned successfully**. This means all tables, sequences, indexes, and seed data have been created.

---

## Step 3: Update Connection String in `appsettings.json`

The Razor Pages application uses Entity Framework Core with Npgsql to connect to PostgreSQL. 
You must update the connection string in `appsettings.json` to point at your newly created `RestaurantReviewsDB`.
1. Open the Project in Your Editor/IDE
    - Launch Visual Studio (or VS Code) and open the folder containing `RestaurantReviewApp.csproj`
2. Locate `appsettings.json` (in the project root).
The default `appsettings.json` likely has a placeholder for `ConnectionStrings`. Modify it as follows:
```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=RestaurantReviewsDB;Username=postgres;Password=YourPostgresPassword"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
- **Host**: `localhost` (or the IP/hostname of your PostgreSQL server).
- **Port**: `5432` (default PostgreSQL port).
- **Database**: `RestaurantReviewsDB` (the name you gave in pgAdmin).
- **Username:** `postgres` (or another PostgreSQL role).
- **Password**: the password you set for the chosen role.

---

## Step 4: Install .NET SDK & Restore Packages
1. Confirm .NET 8.0 SDK Is Installed
In a terminal/PowerShell:
```bash
dotnet --list-sdks
```
You should see something like:
`8.0.100 [C:\Program Files\dotnet\sdk]`
2. Restore NuGet Packages
```bash
dotnet restore
```
This will download all required NuGet packages (EF Core, Npgsql, Identity, ScottPlot, etc.).
3. Build the Project
```bash
dotnet build
```