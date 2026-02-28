# Real-Time Communication

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Technology Decision

| Concern | Choice | Rationale |
|---------|--------|-----------|
| **Protocol** | SignalR (WebSocket + fallbacks) | .NET native, auto-reconnect, typed hubs, group management |
| **Transport Priority** | WebSocket → Server-Sent Events → Long Polling | Best performance first, graceful degradation |
| **Client Library** | @microsoft/signalr 10 | Official TypeScript client, auto-reconnect, typed proxies |
| **Authentication** | JWT bearer via query string on connect | Standard SignalR auth pattern |
| **Scaling** | Redis backplane | Required for multi-instance deployment |

---

## 2. Hub Architecture

```
┌──────────────────────────────────────────────────────┐
│                    SignalR Hubs                        │
│                                                       │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐ │
│  │  KPI Hub    │  │  Alert Hub  │  │  System Hub  │ │
│  │  /hubs/kpi  │  │ /hubs/alerts│  │ /hubs/system │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬───────┘ │
│         │                │                │          │
│         └────────────────┼────────────────┘          │
│                          │                           │
│                  ┌───────▼────────┐                  │
│                  │  Redis         │                  │
│                  │  Backplane     │                  │
│                  └───────┬────────┘                  │
│                          │                           │
│         ┌────────────────┼────────────────┐          │
│         │                │                │          │
│  ┌──────▼──────┐  ┌─────▼──────┐  ┌─────▼──────┐  │
│  │  Instance 1 │  │ Instance 2 │  │ Instance N │  │
│  └─────────────┘  └────────────┘  └────────────┘  │
└──────────────────────────────────────────────────────┘
```

---

## 3. Hub Definitions

### KPI Hub

```csharp
[Authorize]
public class KpiHub : Hub
{
    // Client joins entity-specific group on connect
    public override async Task OnConnectedAsync()
    {
        var entityId = Context.User!.FindFirst("active_entity")!.Value;
        var role = Context.User!.FindFirst(ClaimTypes.Role)!.Value;

        // Join entity group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"entity:{entityId}");

        // Join role-specific group (for targeted broadcasts)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"entity:{entityId}:role:{role}");

        await base.OnConnectedAsync();
    }

    // Client can switch entity context
    public async Task SwitchEntity(string newEntityId)
    {
        var oldEntityId = Context.User!.FindFirst("active_entity")!.Value;

        // Verify user has access to new entity
        if (!await _authService.HasEntityAccess(Context.User!, Guid.Parse(newEntityId)))
            throw new HubException("Access denied to entity");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"entity:{oldEntityId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"entity:{newEntityId}");
    }

    // Subscribe to specific KPI updates
    public async Task SubscribeToKpi(string kpiId)
    {
        var entityId = Context.User!.FindFirst("active_entity")!.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"kpi:{entityId}:{kpiId}");
    }

    public async Task UnsubscribeFromKpi(string kpiId)
    {
        var entityId = Context.User!.FindFirst("active_entity")!.Value;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kpi:{entityId}:{kpiId}");
    }
}
```

### Alert Hub

```csharp
[Authorize]
public class AlertHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var entityId = Context.User!.FindFirst("active_entity")!.Value;
        var role = Context.User!.FindFirst(ClaimTypes.Role)!.Value;

        // User-specific alerts
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Entity-wide alerts
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts:{entityId}");

        // Role-specific alerts
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts:{entityId}:{role}");

        await base.OnConnectedAsync();
    }

    // Client acknowledges an alert
    public async Task AcknowledgeAlert(Guid alertEventId)
    {
        var userId = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        await _mediator.Send(new AcknowledgeAlertCommand(alertEventId, Guid.Parse(userId)));
    }
}
```

### System Hub

```csharp
[Authorize(Roles = "Admin")]
public class SystemHub : Hub
{
    // Admin-only system status updates
    // Background job progress, webhook health, queue depth, etc.
}
```

---

## 4. Server-Side Event Broadcasting

### Event-Driven Publishing

```csharp
// Domain event handler → SignalR broadcast
public class KpiUpdatedEventHandler : INotificationHandler<KpiUpdatedEvent>
{
    private readonly IHubContext<KpiHub> _hubContext;

    public async Task Handle(KpiUpdatedEvent notification, CancellationToken ct)
    {
        var message = new KpiUpdateMessage
        {
            KpiId = notification.KpiId,
            EntityId = notification.EntityId,
            Value = notification.NewValue,
            PreviousValue = notification.OldValue,
            ChangePct = notification.ChangePercentage,
            SnapshotDate = notification.SnapshotDate,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Broadcast to all users watching this entity
        await _hubContext.Clients
            .Group($"entity:{notification.EntityId}")
            .SendAsync("KpiUpdated", message, ct);

        // Also broadcast to KPI-specific subscribers
        await _hubContext.Clients
            .Group($"kpi:{notification.EntityId}:{notification.KpiId}")
            .SendAsync("KpiDetailUpdated", message, ct);
    }
}

// Alert triggered → targeted broadcast
public class AlertTriggeredEventHandler : INotificationHandler<AlertTriggeredEvent>
{
    private readonly IHubContext<AlertHub> _alertHub;

    public async Task Handle(AlertTriggeredEvent notification, CancellationToken ct)
    {
        var message = new AlertMessage
        {
            AlertId = notification.AlertId,
            Severity = notification.Severity,
            Title = notification.Title,
            Message = notification.Message,
            KpiId = notification.KpiId,
            CurrentValue = notification.CurrentValue,
            ThresholdValue = notification.ThresholdValue,
            TriggeredAt = notification.TriggeredAt
        };

        // Send to role-specific groups based on alert configuration
        foreach (var role in notification.TargetRoles)
        {
            await _alertHub.Clients
                .Group($"alerts:{notification.EntityId}:{role}")
                .SendAsync("AlertTriggered", message, ct);
        }
    }
}
```

### Broadcast Types

| Event | Target Group | Message Type | Data |
|-------|-------------|-------------|------|
| KPI value changed | `entity:{id}` | `KpiUpdated` | KPI ID, new value, change % |
| KPI detail update | `kpi:{entityId}:{kpiId}` | `KpiDetailUpdated` | Full KPI snapshot |
| Alert triggered | `alerts:{entityId}:{role}` | `AlertTriggered` | Alert details, severity |
| Alert resolved | `alerts:{entityId}` | `AlertResolved` | Alert ID |
| Document processed | `user:{uploaderId}` | `DocumentProcessed` | Document ID, status, fields |
| Scenario completed | `user:{creatorId}` | `ScenarioCompleted` | Scenario ID, summary |
| DATEV export ready | `entity:{id}:role:finance` | `DatevExportReady` | Export ID, download link |
| Webhook error | `entity:{id}:role:admin` | `WebhookError` | Source, error details |
| System status | Admin group | `SystemStatus` | Health metrics |

---

## 5. Client-Side Integration

### Connection Management

```typescript
// hooks/useSignalR.ts
export function useSignalR() {
    const { accessToken } = useAuth();
    const { selectedEntityId } = useEntityStore();
    const connectionRef = useRef<HubConnection | null>(null);
    const queryClient = useQueryClient();

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl('/hubs/kpi', {
                accessTokenFactory: () => accessToken ?? '',
                transport: HttpTransportType.WebSockets,
                skipNegotiation: true,
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    // Exponential backoff: 0, 2s, 5s, 10s, 30s, then every 30s
                    const delays = [0, 2000, 5000, 10000, 30000];
                    return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)];
                }
            })
            .withHubProtocol(new JsonHubProtocol())
            .configureLogging(LogLevel.Warning)
            .build();

        // KPI updates → update React Query cache directly
        connection.on('KpiUpdated', (update: KpiUpdateMessage) => {
            queryClient.setQueryData(
                ['dashboard', selectedEntityId],
                (old: DashboardDto | undefined) =>
                    old ? applyKpiUpdate(old, update) : old
            );
        });

        // Alerts → show toast + update alert store
        connection.on('AlertTriggered', (alert: AlertMessage) => {
            toast[alert.severity === 'critical' ? 'error' : 'warning'](
                alert.title,
                { description: alert.message }
            );
            useAlertStore.getState().addAlert(alert);
        });

        connection.on('AlertResolved', (data: { alertId: string }) => {
            useAlertStore.getState().resolveAlert(data.alertId);
        });

        // Reconnection handling
        connection.onreconnecting(() => {
            useUiStore.getState().setConnectionStatus('reconnecting');
        });

        connection.onreconnected(() => {
            useUiStore.getState().setConnectionStatus('connected');
            // Refresh stale data after reconnection
            queryClient.invalidateQueries({ queryKey: ['dashboard'] });
        });

        connection.onclose(() => {
            useUiStore.getState().setConnectionStatus('disconnected');
        });

        connection.start().then(() => {
            useUiStore.getState().setConnectionStatus('connected');
        });

        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, selectedEntityId]);

    return connectionRef;
}
```

### Entity Switching

```typescript
// When user switches entity, update SignalR group
export function useEntitySwitch() {
    const connection = useSignalR();

    const switchEntity = useCallback(async (newEntityId: string) => {
        if (connection.current?.state === HubConnectionState.Connected) {
            await connection.current.invoke('SwitchEntity', newEntityId);
        }
        useEntityStore.getState().setSelectedEntity(newEntityId);
    }, [connection]);

    return { switchEntity };
}
```

---

## 6. Scaling with Redis Backplane

### Configuration

```csharp
// Program.cs
services.AddSignalR()
    .AddStackExchangeRedis(Configuration["Redis:ConnectionString"], options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("clarityboard");
    });
```

### How It Works

```
Instance A receives KPI update
        │
        ▼
Instance A publishes to Redis channel: "clarityboard:KpiHub:entity:uuid"
        │
        ▼
Redis distributes to all subscribed instances
        │
        ├──▶ Instance A → broadcasts to local WebSocket clients
        ├──▶ Instance B → broadcasts to local WebSocket clients
        └──▶ Instance C → broadcasts to local WebSocket clients
```

---

## 7. Connection Monitoring

| Metric | Measurement | Alert Threshold |
|--------|-------------|----------------|
| Active connections | Gauge per hub | > 1000 per instance |
| Messages/sec | Counter per hub | > 500/sec sustained |
| Connection errors | Counter | > 10/min |
| Reconnection rate | Counter | > 50/min |
| Message latency P95 | Histogram | > 100ms |
| Redis backplane lag | Histogram | > 50ms |

### Health Check

```csharp
public class SignalRHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        var activeConnections = _connectionTracker.GetActiveCount();
        var redisConnected = await _redis.PingAsync();

        if (!redisConnected)
            return HealthCheckResult.Unhealthy("Redis backplane disconnected");

        return HealthCheckResult.Healthy($"Active connections: {activeConnections}");
    }
}
```

---

## Document Navigation

- Previous: [Authentication & Authorization](./07-auth-architecture.md)
- Next: [Caching & Performance](./09-caching-performance.md)
- [Back to Index](./README.md)
