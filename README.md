# File Sharing API - Backend Architecture

A robust, containerized ASP.NET Core 9.0 backend built to handle secure file uploads, automated storage management, and metadata tracking. This repository contains the backend services, database configuration, and CI/CD pipelines.

## 🚀 Tech Stack
* **Framework:** C# / ASP.NET Core 9.0
* **Database:** PostgreSQL (via Entity Framework Core)
* **Containerization:** Docker & Docker Compose
* **Testing:** xUnit & Moq
* **CI/CD:** GitHub Actions -> Render

## ⚙️ Key Features & Architecture
* **Repository Pattern:** Strictly separates business logic from database interactions, allowing for highly modular and testable code.
* **Automated Background Janitor:** Utilizes a custom `BackgroundService` with a `PeriodicTimer` and Scoped Dependency Injection to automatically delete expired files and database records every hour, completely independent of web traffic.
* **Optimized File I/O:** Bypasses the database for physical file storage, writing raw byte streams directly to the local disk/container volume for maximum performance.
* **Multi-Stage Docker Builds:** Compiles using the heavy .NET SDK but publishes to a lightweight ASP.NET runtime image, keeping production deployments fast and secure.
* **Robust Validation:** Enforces strict 10MB upload limits and auto-generates unique 6-character GUID-based claim codes for secure sharing.

## 🛠️ How to Run Locally

To test the full API and Database environment locally without affecting the live production server, you can use Docker Compose.

**Prerequisites:**
* Docker Desktop installed and running.

**Steps:**
1. Clone this repository to your local machine.
2. Open a terminal in the root directory (where the docker-compose.yml file is located).
3. Run the following command:
`docker-compose up -d`
4. Docker will automatically pull the postgres:14-alpine image, build the API container, and link them together. 
5. The API will be accessible locally, and physical files/database rows will persist securely on your host machine via Docker Volumes.

## 🧪 Testing
This project strictly enforces unit testing for all business logic using **xUnit** and **Moq**.
By mocking the `IFileRepository` and `IStorageService` interfaces, the `FilesController` is tested in total isolation—preventing test data from polluting the live database or filling up the hard drive.

To run the test suite:
`dotnet test`

## 🔄 CI/CD Pipeline
This repository uses GitHub Actions for continuous integration and continuous deployment.
* **On Push to main:** The pipeline automatically provisions an Ubuntu server, restores .NET 9 dependencies, and runs the entire xUnit test suite.
* **Deployment Guard:** If any test fails, the pipeline halts. If all tests pass, it builds the Docker image and pushes the live code to the production environment on Render.

asdasdaas