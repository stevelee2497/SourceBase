# Rate Limiting

## Use Case

Protect the API from bot abuse, credential stuffing, and email-spam attacks by enforcing per-IP request rate limits. Two tiers of protection are applied:

- A **general** policy covers all `/api` endpoints.
- A **strict** policy applies a tighter limit to sensitive anonymous auth endpoints.

## Policies

| Policy    | Permit Limit | Window | Endpoints                                                                                                                       |
| --------- | ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------- |
| `general` | 100 requests | 60 s   | All `/api` endpoints                                                                                                            |
| `strict`  | 10 requests  | 60 s   | `auth/login`, `auth/register`, `auth/forgotPassword`, `auth/confirmEmail`, `auth/resendConfirmationEmail`, `auth/resetPassword` |

Both limits are configurable via `AppSettings:RateLimitSettings` in `appsettings.json`.

## Partition Key

Rate limiting is keyed **per IP address**. The real client IP is resolved in this order:

1. `X-Forwarded-For` request header (populated by Caddy reverse proxy)
2. `HttpContext.Connection.RemoteIpAddress` (direct connections)

`UseForwardedHeaders` middleware is required in the pipeline before rate limiting so that `RemoteIpAddress` reflects the real client IP.

## Request / Response

No changes to request shape. When a limit is exceeded:

- **HTTP Status:** `429 Too Many Requests`
- **Header:** `Retry-After: <seconds>`
- **Body:** consistent with `GlobalExceptionMiddleware` format

```json
{
  "traceId": "0HN6QFPV9JM2C:00000001",
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Too many requests. Please try again later.",
  "errors": {}
}
```

## Failure Modes

| Scenario                                          | Behaviour                       |
| ------------------------------------------------- | ------------------------------- |
| Client sends > `strict` limit requests in window  | 429 with Retry-After            |
| Client sends > `general` limit requests in window | 429 with Retry-After            |
| Request behind proxy without `X-Forwarded-For`    | Falls back to `RemoteIpAddress` |
| `RemoteIpAddress` is null                         | Uses partition key `"unknown"`  |

## Configuration

```json
"AppSettings": {
  "RateLimitSettings": {
    "GeneralPermitLimit": 100,
    "GeneralWindowSeconds": 60,
    "StrictPermitLimit": 10,
    "StrictWindowSeconds": 60
  }
}
```

## Implementation Notes

- Uses `System.Threading.RateLimiting.FixedWindowRateLimiter` — part of the .NET runtime, no extra NuGet packages.
- `UseForwardedHeaders` must be called before `UseRateLimiting` in the middleware pipeline.
- The `strict` policy stacks on top of `general` for the targeted endpoints — both limits are enforced independently.
