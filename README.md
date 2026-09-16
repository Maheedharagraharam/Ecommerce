# E-Commerce Microservices Backend (.NET 9 & Microsoft Azure)

A production-style, distributed microservices backend built with **.NET 9 Web API**, **Microsoft Azure Cloud Services**, **Docker**, and **Microsoft YARP**.

This project is architected to showcase core distributed systems design patterns, inter-service communication (both synchronous REST and asynchronous event-driven messaging), cloud storage, and fault-tolerance policies.

---

## Architecture Diagram

```
                       [ Client / Frontend / Postman ]
                                      │
                                      ▼
                       ┌──────────────────────────────┐
                       │      API Gateway (YARP)      │ Port 5000 (Single entry point)
                       └──────────────┬───────────────┘
                                      │
           ┌──────────────────────────┴──────────────────────────┐
           │ (Route: /api/products)                              │ (Route: /api/orders)
           ▼                                                     ▼
┌──────────────────────────────┐              ┌──────────────────────────────┐
│       Catalog Service        │              │       Ordering Service       │
│         (Port 5001)          │◄─────────────┤         (Port 5002)          │
│ • EF Core SQLite             │  HTTP Sync   │ • EF Core SQLite             │
│ • Azure Blob Storage (Images)│ (with Polly) │ • Synchronous Product Check  │
└──────────────────────────────┘              │ • Publishes OrderCreatedEvent│
                                              └──────────────┬───────────────┘
                                                             │
                                                             │ Async Event Message
                                                             ▼
                                              ┌──────────────────────────────┐
                                              │    Azure Service Bus /       │
                                              │      MassTransit Bus         │
                                              └──────────────┬───────────────┘
                                                             │
                                                             │ Subscribes to event
                                                             ▼
                                              ┌──────────────────────────────┐
                                              │     Notification Worker      │
                                              │         (Port 5003)          │
                                              │ • Consumes OrderCreatedEvent │
                                              │ • Uploads Invoice to Azure   │
                                              │ • Simulates Customer Email   │
                                              └──────────────────────────────┘
```

---

## Key Technologies & Why Employers Love Them

| Technology | Role in Architecture | Why It's In-Demand |
| :--- | :--- | :--- |
| **.NET 9 Web API** | Core framework | Latest modern .NET runtime, high throughput, standard for enterprise backends. |
| **Microsoft YARP** | API Gateway & Reverse Proxy | Official Microsoft reverse proxy, handles route matching, header forwarding, and rate limiting. |
| **MassTransit** | Message Broker Abstraction | #1 enterprise messaging framework in .NET. Enables seamless switching between local In-Memory and cloud Azure Service Bus. |
| **Azure Blob Storage** | Media & Document Cloud Storage | Official `Azure.Storage.Blobs` SDK used to upload product media and generated invoices. |
| **Azure Service Bus** | Cloud Message Queue | Enterprise pub/sub messaging topic broker for decoupled microservices. |
| **Polly** | HTTP Resilience & Fault Tolerance | Automatic retries with exponential backoff and circuit breaker protection during inter-service HTTP communication. |
| **Entity Framework Core 9** | Object Relational Mapper (ORM) | SQLite Code-First approach with automatic schema creation and migrations. |
| **Docker & Docker Compose** | Containerization | Multi-stage Docker builds and cross-service container networking. |

---

## Understanding the Inter-Service Communication

### 1. Synchronous Communication (HTTP REST with Polly)
* **When**: When a user calls `POST /api/orders`, the **Ordering Service** immediately queries the **Catalog Service** (`GET /api/products/{id}`) to verify that the product exists, fetch the current price, and verify stock availability.
* **Resilience**: The typed `CatalogServiceClient` wraps requests in **Polly policies**:
  - **Retry Policy**: Retries transient network failures 3 times with exponential backoff (2s, 4s, 8s).
  - **Circuit Breaker**: Trips and stops traffic for 30 seconds if 5 consecutive failures occur, preventing cascade failures across services.

### 2. Asynchronous Communication (Event-Driven via MassTransit)
* **When**: Once an order is saved in the database, the Ordering Service publishes an `OrderCreatedEvent` using `IPublishEndpoint.Publish()`.
* **Decoupling**: The Ordering Service returns `201 Created` immediately without waiting for emails to send.
* **Consumer**: The **Notification Worker** asynchronously receives the event from the message bus, generates an invoice receipt, uploads it to Azure Blob Storage, and dispatches a confirmation email.

---

## Azure Integration Guide

### 1. Azure Blob Storage
* Used in **Catalog.API** (for product images) and **Notification.Worker** (for customer invoice files).
* **Configuration** in `appsettings.json`:
  ```json
  "AzureStorage": {
    "ConnectionString": "<YOUR_AZURE_STORAGE_CONNECTION_STRING>",
    "ContainerName": "product-images"
  }
  ```
* **Offline Fallback**: If the connection string is left blank, the application automatically uses an internal local file storage simulator (`/uploads`), allowing full testing without needing an active Azure credit card!

### 2. Azure Service Bus
* Used for enterprise decoupled event messaging between microservices.
* **Configuration** in `appsettings.json`:
  ```json
  "AzureServiceBus": {
    "ConnectionString": "<YOUR_AZURE_SERVICE_BUS_CONNECTION_STRING>"
  }
  ```
* **Offline Fallback**: If left blank, MassTransit automatically defaults to its built-in **In-Memory transport**, functioning with zero configuration.

---

## How to Run Locally

### Option A: Run All Services at Once (PowerShell)
Run the automated launcher script from the root directory:
```powershell
.\run-all.ps1
```
This opens 4 terminal windows and launches all services simultaneously.

### Option B: Run via Docker Compose
Ensure Docker Desktop is installed and running, then execute:
```bash
docker compose up --build
```

### Option C: Run Each Service Manually
In separate terminal windows:
```powershell
# 1. Start Catalog Service (Port 5001)
cd src/Services/Catalog/Catalog.API
dotnet run

# 2. Start Ordering Service (Port 5002)
cd src/Services/Ordering/Ordering.API
dotnet run

# 3. Start Notification Worker (Port 5003)
cd src/Services/Notification/Notification.Worker
dotnet run

# 4. Start API Gateway (Port 5000)
cd src/Gateway/ApiGateway
dotnet run
```

---

## Service Endpoints & Swagger UIs

| Service | Base URL | Swagger Documentation |
| :--- | :--- | :--- |
| **API Gateway (Entry Point)** | `http://localhost:5000` | Routes `/api/products`, `/api/orders`, `/api/notifications` |
| **Catalog Microservice** | `http://localhost:5001` | `http://localhost:5001/swagger` |
| **Ordering Microservice** | `http://localhost:5002` | `http://localhost:5002/swagger` |
| **Notification Worker** | `http://localhost:5003` | `http://localhost:5003/swagger` |

---

## End-to-End API Testing Guide

### 1. View Seeded Products (via API Gateway)
```bash
curl http://localhost:5000/api/products
```

### 2. Add a New Product (via API Gateway)
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Noise Cancelling Headphones",
    "description": "Premium wireless headphones",
    "price": 199.99,
    "stockQuantity": 50,
    "imageUrl": "https://example.com/headphones.jpg"
  }'
```

### 3. Place an Order (Triggers Sync HTTP check + Async Event Bus)
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerEmail": "john.doe@example.com",
    "customerName": "John Doe",
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "quantity": 2
      }
    ]
  }'
```

### 4. Verify Processed Notifications (Dispatched asynchronously)
```bash
curl http://localhost:5000/api/notifications
```

---

## Ready-to-Use Resume Section

Copy and adapt these bullet points for your resume:

> ### **Full-Stack / .NET Cloud Backend Developer**
> * **Architected an Event-Driven Microservices Backend** using **.NET 9 Web API**, breaking domain responsibilities into Catalog, Ordering, and Notification services with individual SQLite/EF Core data stores.
> * **Designed Single Entry API Gateway** using **Microsoft YARP**, orchestrating route mapping, header propagation, and centralized reverse proxying.
> * **Engineered Synchronous & Asynchronous Inter-Service Communication**:
>   - Implemented typed `HttpClient` communication with **Polly** resilience policies (exponential backoff retry and circuit breaker).
>   - Integrated **MassTransit** over **Azure Service Bus** to asynchronously publish and consume domain events (`OrderCreatedEvent`), decoupling order ingestion from invoicing and email dispatch.
> * **Integrated Azure Cloud Storage**: Built cloud persistence with **Azure Blob Storage** (`Azure.Storage.Blobs`) for product media and dynamic invoice receipts with local fallback simulators.
> * **Containerized & Automated CI/CD**: Authored multi-stage **Dockerfiles**, unified container networking with **Docker Compose**, and created automated **GitHub Actions** CI pipelines.

---

## Common Technical Interview Questions (And How to Answer Them)

### Q: Why did you use both Synchronous and Asynchronous communication?
**A:** Synchronous HTTP is used when an immediate, transactional response is mandatory—the Ordering Service needs real-time price and stock confirmation from the Catalog Service before confirming the transaction. Asynchronous messaging via MassTransit/Azure Service Bus is used for operations that do not block the user (such as invoice generation and email notifications), ensuring high throughput and resilience against downstream notification outages.

### Q: Why use YARP instead of Ocelot?
**A:** Microsoft YARP (Yet Another Reverse Proxy) is developed and maintained directly by Microsoft, offering superior performance on modern .NET (leveraging HTTP/2 and HTTP/3 optimizations) with native ASP.NET Core middleware integration.

### Q: How did you handle transient failures during inter-service calls?
**A:** By incorporating Polly policies on the typed `HttpClient`, configuring exponential backoff retries for transient HTTP errors (status codes 5xx and 408) and circuit breakers to fail-fast when a downstream service is unresponsive.
