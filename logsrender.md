2025-10-17T08:20:24.856469539Z          at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
2025-10-17T08:20:24.856471309Z          at Congreso.Api.Controllers.AuthController.Register(RegisterDto dto) in /src/Controllers/AuthController.cs:line 401
2025-10-17T08:20:24.856472959Z fail: Congreso.Api.Controllers.AuthController[0]
2025-10-17T08:20:24.856474619Z       Inner: 42P01: relation "student_accounts" does not exist
2025-10-17T08:20:24.856476329Z       
2025-10-17T08:20:24.856478039Z       POSITION: 13
2025-10-17T08:20:24.887400851Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:24.887421442Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T08:20:24.887Z","level":"error","service":"Congreso.Api","trace_id":"49df5852-c97e-4956-82e0-0c8bb30f204c","request_id":"8cd52a11-5981-441c-8082-41357ed0e874","event_type":"request_complete","duration_ms":2863,"http":{"method":"POST","endpoint":"/api/auth/register","status_code":500,"user_id":"anonymous","ip_address":"10.229.95.193"}}
2025-10-17T08:20:30.820607026Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:30.820635987Z       REQUEST_START {"timestamp":"2025-10-17T08:20:30.820Z","level":"info","service":"Congreso.Api","trace_id":"aff70493-738c-456b-873a-dbf658bbebd5","request_id":"85934120-ac3f-44c8-b0f5-76965e8fe566","event_type":"request_start","http":{"method":"POST","endpoint":"/api/auth/login","user_agent":"curl/8.7.1","ip_address":"10.229.95.193","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T08:20:30.820840809Z info: Congreso.Api.Middleware.RateLimitingMiddleware[0]
2025-10-17T08:20:30.820852189Z       Rate limit attempt: 10.229.95.193 /api/auth/login POST 1/5
2025-10-17T08:20:30.889152995Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T08:20:30.889187807Z       Executed DbCommand (66ms) [Parameters=[@__dto_Email_0='?'], CommandType='Text', CommandTimeout='60']
2025-10-17T08:20:30.889197388Z       SELECT u.id_guid, u.avatar_url, u.created_at, u.email, u.full_name, u.is_active, u.is_umg, u.last_login_at, u.org_id, u.org_name, u.password_hash, u.status, u.updated_at
2025-10-17T08:20:30.889202958Z       FROM users AS u
2025-10-17T08:20:30.889207928Z       WHERE u.email = @__dto_Email_0
2025-10-17T08:20:30.889212388Z       LIMIT 1
2025-10-17T08:20:30.889802421Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:30.889818313Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T08:20:30.889Z","level":"warning","service":"Congreso.Api","trace_id":"aff70493-738c-456b-873a-dbf658bbebd5","request_id":"85934120-ac3f-44c8-b0f5-76965e8fe566","event_type":"request_complete","duration_ms":69,"http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.95.193"}}
2025-10-17T08:20:30.889823313Z warn: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:30.889827403Z       UNAUTHORIZED_ACCESS {"timestamp":"2025-10-17T08:20:30.889Z","level":"warning","service":"Congreso.Api","trace_id":"aff70493-738c-456b-873a-dbf658bbebd5","request_id":"85934120-ac3f-44c8-b0f5-76965e8fe566","event_type":"unauthorized_access","http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.95.193"}}
2025-10-17T08:20:43.236110902Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:43.236140934Z       REQUEST_START {"timestamp":"2025-10-17T08:20:43.235Z","level":"info","service":"Congreso.Api","trace_id":"fe287241-1fc8-4862-b60c-897005787a14","request_id":"89117f3d-be03-4488-b5a2-5326bc4eec0b","event_type":"request_start","http":{"method":"POST","endpoint":"/api/auth/login","user_agent":"curl/8.7.1","ip_address":"10.229.243.66","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T08:20:43.236152785Z info: Congreso.Api.Middleware.RateLimitingMiddleware[0]
2025-10-17T08:20:43.236157345Z       Rate limit attempt: 10.229.243.66 /api/auth/login POST 1/5
2025-10-17T08:20:43.304314263Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T08:20:43.304333044Z       Executed DbCommand (66ms) [Parameters=[@__dto_Email_0='?'], CommandType='Text', CommandTimeout='60']
2025-10-17T08:20:43.304336844Z       SELECT u.id_guid, u.avatar_url, u.created_at, u.email, u.full_name, u.is_active, u.is_umg, u.last_login_at, u.org_id, u.org_name, u.password_hash, u.status, u.updated_at
2025-10-17T08:20:43.304340214Z       FROM users AS u
2025-10-17T08:20:43.304343434Z       WHERE u.email = @__dto_Email_0
2025-10-17T08:20:43.304346385Z       LIMIT 1
2025-10-17T08:20:43.304899625Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:43.304909286Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T08:20:43.304Z","level":"warning","service":"Congreso.Api","trace_id":"fe287241-1fc8-4862-b60c-897005787a14","request_id":"89117f3d-be03-4488-b5a2-5326bc4eec0b","event_type":"request_complete","duration_ms":68,"http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.243.66"}}
2025-10-17T08:20:43.304912616Z warn: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:43.304915506Z       UNAUTHORIZED_ACCESS {"timestamp":"2025-10-17T08:20:43.304Z","level":"warning","service":"Congreso.Api","trace_id":"fe287241-1fc8-4862-b60c-897005787a14","request_id":"89117f3d-be03-4488-b5a2-5326bc4eec0b","event_type":"unauthorized_access","http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.243.66"}}
2025-10-17T08:20:49.24112696Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:49.241161662Z       REQUEST_START {"timestamp":"2025-10-17T08:20:49.240Z","level":"info","service":"Congreso.Api","trace_id":"a55f324a-64ec-4849-aa2f-d115ac57e4f9","request_id":"8ee472a4-17ce-4cea-a296-e71c72114934","event_type":"request_start","http":{"method":"POST","endpoint":"/api/auth/login","user_agent":"curl/8.7.1","ip_address":"10.229.95.193","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T08:20:49.241166343Z info: Congreso.Api.Middleware.RateLimitingMiddleware[0]
2025-10-17T08:20:49.241170993Z       Rate limit attempt: 10.229.95.193 /api/auth/login POST 2/5
2025-10-17T08:20:49.30914183Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T08:20:49.309164331Z       Executed DbCommand (66ms) [Parameters=[@__dto_Email_0='?'], CommandType='Text', CommandTimeout='60']
2025-10-17T08:20:49.309170102Z       SELECT u.id_guid, u.avatar_url, u.created_at, u.email, u.full_name, u.is_active, u.is_umg, u.last_login_at, u.org_id, u.org_name, u.password_hash, u.status, u.updated_at
2025-10-17T08:20:49.309174942Z       FROM users AS u
2025-10-17T08:20:49.309179663Z       WHERE u.email = @__dto_Email_0
2025-10-17T08:20:49.309196673Z       LIMIT 1
2025-10-17T08:20:49.309896423Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:49.310321636Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T08:20:49.309Z","level":"warning","service":"Congreso.Api","trace_id":"a55f324a-64ec-4849-aa2f-d115ac57e4f9","request_id":"8ee472a4-17ce-4cea-a296-e71c72114934","event_type":"request_complete","duration_ms":68,"http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.95.193"}}
2025-10-17T08:20:49.310329607Z warn: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T08:20:49.310332777Z       UNAUTHORIZED_ACCESS {"timestamp":"2025-10-17T08:20:49.309Z","level":"warning","service":"Congreso.Api","trace_id":"a55f324a-64ec-4849-aa2f-d115ac57e4f9","request_id":"8ee472a4-17ce-4cea-a296-e71c72114934","event_type":"unauthorized_access","http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"10.229.95.193"}}