var builder = WebApplication.CreateBuilder(args);

// 1. Add Microsoft YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 3. Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowAll");

// Gateway root route: Returns an interactive HTML dashboard in a browser, or JSON for API clients
app.MapGet("/", (HttpContext context) =>
{
    var accept = context.Request.Headers.Accept.ToString();
    if (accept.Contains("text/html"))
    {
        var html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Microservices API Gateway</title>
            <style>
                :root { --bg: #0f172a; --card: #1e293b; --text: #f8fafc; --accent: #38bdf8; --success: #4ade80; --border: #334155; }
                body { font-family: 'Segoe UI', system-ui, sans-serif; background: var(--bg); color: var(--text); margin: 0; padding: 40px 20px; display: flex; justify-content: center; }
                .container { max-width: 900px; width: 100%; }
                .header { display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border); padding-bottom: 20px; margin-bottom: 30px; }
                .title { font-size: 24px; font-weight: 700; color: var(--accent); margin: 0; }
                .badge { background: rgba(74, 222, 128, 0.15); color: var(--success); padding: 6px 14px; border-radius: 9999px; font-size: 13px; font-weight: 600; border: 1px solid rgba(74, 222, 128, 0.3); }
                .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 20px; margin-bottom: 30px; }
                .card { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 20px; transition: transform 0.2s; }
                .card:hover { transform: translateY(-3px); border-color: var(--accent); }
                .card-title { font-size: 18px; font-weight: 600; margin: 0 0 8px 0; color: #fff; }
                .card-port { font-size: 13px; color: #94a3b8; margin-bottom: 12px; }
                .card-desc { font-size: 14px; color: #cbd5e1; margin-bottom: 16px; line-height: 1.5; }
                .btn { display: inline-block; background: var(--accent); color: #0f172a; text-decoration: none; padding: 8px 16px; border-radius: 6px; font-weight: 600; font-size: 13px; }
                .btn-secondary { background: #334155; color: #fff; margin-left: 8px; }
                .routes-table { width: 100%; border-collapse: collapse; background: var(--card); border-radius: 12px; overflow: hidden; border: 1px solid var(--border); }
                .routes-table th, .routes-table td { padding: 14px 18px; text-align: left; font-size: 14px; }
                .routes-table th { background: #162032; color: #94a3b8; font-weight: 600; border-bottom: 1px solid var(--border); }
                .routes-table td { border-bottom: 1px solid var(--border); }
                .routes-table tr:last-child td { border-bottom: none; }
                .code { font-family: monospace; background: #0b1120; padding: 3px 8px; border-radius: 4px; color: #38bdf8; }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <div>
                        <h1 class="title">🚀 Microservices API Gateway</h1>
                        <p style="color: #94a3b8; margin: 6px 0 0 0; font-size: 14px;">Powered by Microsoft YARP (Yet Another Reverse Proxy)</p>
                    </div>
                    <span class="badge">● Status: Healthy</span>
                </div>

                <h3 style="color: #cbd5e1; margin-bottom: 16px;">Interactive Swagger Documentation UIs</h3>
                <div class="grid">
                    <div class="card">
                        <h4 class="card-title">Catalog Microservice</h4>
                        <div class="card-port">Port 5001 &bull; SQLite & Azure Blob</div>
                        <p class="card-desc">Product catalog management, inventory tracking, and cloud image uploads.</p>
                        <a href="http://localhost:5001/swagger" target="_blank" class="btn">Open Swagger UI &rarr;</a>
                    </div>
                    <div class="card">
                        <h4 class="card-title">Ordering Microservice</h4>
                        <div class="card-port">Port 5002 &bull; Sync HTTP & Polly</div>
                        <p class="card-desc">Place orders with real-time Catalog verification and MassTransit event publishing.</p>
                        <a href="http://localhost:5002/swagger" target="_blank" class="btn">Open Swagger UI &rarr;</a>
                    </div>
                    <div class="card">
                        <h4 class="card-title">Notification Worker</h4>
                        <div class="card-port">Port 5003 &bull; Azure Service Bus</div>
                        <p class="card-desc">Consumes OrderCreated events, uploads PDF invoices to Azure, and dispatches emails.</p>
                        <a href="http://localhost:5003/swagger" target="_blank" class="btn">Open Swagger UI &rarr;</a>
                    </div>
                </div>

                <h3 style="color: #cbd5e1; margin-bottom: 16px;">Gateway Reverse-Proxy Routes (Port 5000)</h3>
                <table class="routes-table">
                    <thead>
                        <tr>
                            <th>Gateway Route</th>
                            <th>Target Microservice</th>
                            <th>Quick Test</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td><span class="code">/api/products</span></td>
                            <td>Catalog Service (Port 5001)</td>
                            <td><a href="/api/products" target="_blank" style="color: #38bdf8; text-decoration: none;">View JSON &rarr;</a></td>
                        </tr>
                        <tr>
                            <td><span class="code">/api/orders</span></td>
                            <td>Ordering Service (Port 5002)</td>
                            <td><a href="/api/orders" target="_blank" style="color: #38bdf8; text-decoration: none;">View Orders &rarr;</a></td>
                        </tr>
                        <tr>
                            <td><span class="code">/api/notifications</span></td>
                            <td>Notification Worker (Port 5003)</td>
                            <td><a href="/api/notifications" target="_blank" style="color: #38bdf8; text-decoration: none;">View Notifications &rarr;</a></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </body>
        </html>
        """;
        return Results.Content(html, "text/html");
    }

    return Results.Json(new
    {
        service = "API Gateway (Microsoft YARP)",
        status = "Healthy",
        port = 5000,
        routes = new[]
        {
            new { route = "/api/products", target = "Catalog Service (Port 5001)", description = "Product catalog CRUD & Azure Blob image upload" },
            new { route = "/api/orders", target = "Ordering Service (Port 5002)", description = "Place orders with sync HTTP verification & async event bus publishing" },
            new { route = "/api/notifications", target = "Notification Worker (Port 5003)", description = "View received order notification events" }
        },
        swaggerEndpoints = new[]
        {
            "http://localhost:5001/swagger (Catalog API)",
            "http://localhost:5002/swagger (Ordering API)",
            "http://localhost:5003/swagger (Notification Worker)"
        }
    });
});

app.MapHealthChecks("/health");

// Map YARP Reverse Proxy routes
app.MapReverseProxy();

app.Run();
