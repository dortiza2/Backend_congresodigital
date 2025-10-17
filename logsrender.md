2025-10-17T03:26:17.358819355Z       UNAUTHORIZED_ACCESS {"timestamp":"2025-10-17T03:26:17.356Z","level":"warning","service":"Congreso.Api","trace_id":"06f8ad5d-7784-4be9-a6d5-10954671a12b","request_id":"ba3737ca-04f5-4210-850f-35cf7ebae896","event_type":"unauthorized_access","http":{"method":"POST","endpoint":"/api/auth/login","status_code":401,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:26:39.693661655Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:26:39.693682696Z       REQUEST_START {"timestamp":"2025-10-17T03:26:39.693Z","level":"info","service":"Congreso.Api","trace_id":"3fdc4afd-ed1b-437f-8102-f80503416bea","request_id":"d058432d-ed00-407a-b837-74420ae675ba","event_type":"request_start","http":{"method":"GET","endpoint":"/api/auth/register","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T03:26:39.693703466Z info: Congreso.Api.Middleware.RateLimitingMiddleware[0]
2025-10-17T03:26:39.693706846Z       Rate limit attempt: ::1 /api/auth/register GET 1/5
2025-10-17T03:26:39.694467941Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:26:39.694475431Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:26:39.694Z","level":"warning","service":"Congreso.Api","trace_id":"3fdc4afd-ed1b-437f-8102-f80503416bea","request_id":"d058432d-ed00-407a-b837-74420ae675ba","event_type":"request_complete","duration_ms":1,"http":{"method":"GET","endpoint":"/api/auth/register","status_code":405,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:26:39.742767653Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:26:39.742788164Z       REQUEST_START {"timestamp":"2025-10-17T03:26:39.742Z","level":"info","service":"Congreso.Api","trace_id":"0e750c11-4d56-493e-8033-6e3688f69a58","request_id":"0fa8a11a-2073-41e8-97fc-877d4fd0f201","event_type":"request_start","http":{"method":"GET","endpoint":"/api/auth/login","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T03:26:39.742795833Z info: Congreso.Api.Middleware.RateLimitingMiddleware[0]
2025-10-17T03:26:39.742797744Z       Rate limit attempt: ::1 /api/auth/login GET 1/5
2025-10-17T03:26:39.743263272Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:26:39.743270142Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:26:39.743Z","level":"warning","service":"Congreso.Api","trace_id":"0e750c11-4d56-493e-8033-6e3688f69a58","request_id":"0fa8a11a-2073-41e8-97fc-877d4fd0f201","event_type":"request_complete","duration_ms":0,"http":{"method":"GET","endpoint":"/api/auth/login","status_code":405,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:29:00.181041137Z ==> Detected service running on port 10000
2025-10-17T03:29:00.300068307Z ==> Docs on specifying a port: https://render.com/docs/web-services#port-binding
2025-10-17T03:41:41.079108131Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:41:41.079131081Z       Application is shutting down...
2025-10-17T03:45:25.493648377Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:45:25.493651577Z       Application started. Press Ctrl+C to shut down.
2025-10-17T03:45:25.493737459Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:45:25.493745109Z       Hosting environment: Production
2025-10-17T03:45:25.493748659Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:45:25.493752359Z       Content root path: /app
2025-10-17T03:45:29.79441504Z warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
2025-10-17T03:45:29.794443561Z       Failed to determine the https port for redirect.
2025-10-17T03:45:29.903665193Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:29.903696004Z       REQUEST_START {"timestamp":"2025-10-17T03:45:29.795Z","level":"info","service":"Congreso.Api","trace_id":"2d529bd0-5dfe-49b5-a766-eacec5b91e66","request_id":"c3996577-32b9-47b7-9fc4-bee008eaa022","event_type":"request_start","http":{"method":"HEAD","endpoint":"/healthz","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:144.0) Gecko/20100101 Firefox/144.0","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":"https://congreso-api.onrender.com/healthz"}}
2025-10-17T03:45:30.391182691Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:30.391200972Z       REQUEST_START {"timestamp":"2025-10-17T03:45:30.390Z","level":"info","service":"Congreso.Api","trace_id":"b8272c4f-8dee-466a-9ce7-2b3d4605a59f","request_id":"564efae4-9128-4b5c-b021-db64b1aa56d8","event_type":"request_start","http":{"method":"HEAD","endpoint":"/healthz","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:144.0) Gecko/20100101 Firefox/144.0","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":"https://congreso-api.onrender.com/healthz"}}
2025-10-17T03:45:33.090739564Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:33.090789314Z       REQUEST_START {"timestamp":"2025-10-17T03:45:33.090Z","level":"info","service":"Congreso.Api","trace_id":"5309598a-157c-4b9a-afb1-5e60109fcd69","request_id":"66865031-818d-4a7d-955e-6f6970b015b6","event_type":"request_start","http":{"method":"HEAD","endpoint":"/healthz","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:144.0) Gecko/20100101 Firefox/144.0","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":"https://congreso-api.onrender.com/healthz"}}
2025-10-17T03:45:35.092671205Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:35.092697975Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:45:35.002Z","level":"info","service":"Congreso.Api","trace_id":"b8272c4f-8dee-466a-9ce7-2b3d4605a59f","request_id":"564efae4-9128-4b5c-b021-db64b1aa56d8","event_type":"request_complete","duration_ms":4611,"http":{"method":"HEAD","endpoint":"/healthz","status_code":200,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:45:35.092714326Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:35.092716595Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:45:35.002Z","level":"info","service":"Congreso.Api","trace_id":"2d529bd0-5dfe-49b5-a766-eacec5b91e66","request_id":"c3996577-32b9-47b7-9fc4-bee008eaa022","event_type":"request_complete","duration_ms":5206,"http":{"method":"HEAD","endpoint":"/healthz","status_code":200,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:45:35.092718326Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:35.092720206Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:45:35.002Z","level":"info","service":"Congreso.Api","trace_id":"5309598a-157c-4b9a-afb1-5e60109fcd69","request_id":"66865031-818d-4a7d-955e-6f6970b015b6","event_type":"request_complete","duration_ms":1911,"http":{"method":"HEAD","endpoint":"/healthz","status_code":200,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:45:35.193015293Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:35.193034904Z       REQUEST_START {"timestamp":"2025-10-17T03:45:35.192Z","level":"info","service":"Congreso.Api","trace_id":"03269d43-5a85-4295-b079-483ea363dee8","request_id":"84b4a457-4a0d-4cbd-ac9c-7492c6d8efd5","event_type":"request_start","http":{"method":"GET","endpoint":"/healthz","user_agent":"Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:144.0) Gecko/20100101 Firefox/144.0","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":"https://congreso-api.onrender.com/healthz"}}
2025-10-17T03:45:35.257695535Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:45:35.257720825Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:45:35.257Z","level":"info","service":"Congreso.Api","trace_id":"03269d43-5a85-4295-b079-483ea363dee8","request_id":"84b4a457-4a0d-4cbd-ac9c-7492c6d8efd5","event_type":"request_complete","duration_ms":64,"http":{"method":"GET","endpoint":"/healthz","status_code":200,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:56:29.863536315Z ==> Deploying...
2025-10-17T03:56:43.140362084Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T03:56:43.140409905Z       Executed DbCommand (162ms) [Parameters=[], CommandType='Text', CommandTimeout='60']
2025-10-17T03:56:43.140417365Z       SELECT "MigrationId", "ProductVersion"
2025-10-17T03:56:43.140419235Z       FROM "__EFMigrationsHistory"
2025-10-17T03:56:43.140467236Z       ORDER BY "MigrationId";
2025-10-17T03:56:45.329863Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T03:56:45.32988568Z       Executed DbCommand (90ms) [Parameters=[], CommandType='Text', CommandTimeout='60']
2025-10-17T03:56:45.329889281Z       CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
2025-10-17T03:56:45.329892371Z           "MigrationId" character varying(150) NOT NULL,
2025-10-17T03:56:45.329895031Z           "ProductVersion" character varying(32) NOT NULL,
2025-10-17T03:56:45.329898021Z           CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
2025-10-17T03:56:45.329900861Z       );
2025-10-17T03:56:45.338274851Z info: Microsoft.EntityFrameworkCore.Migrations[20411]
2025-10-17T03:56:45.338287911Z       Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
2025-10-17T03:56:45.405837083Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T03:56:45.405859514Z       Executed DbCommand (67ms) [Parameters=[], CommandType='Text', CommandTimeout='60']
2025-10-17T03:56:45.405862804Z       LOCK TABLE "__EFMigrationsHistory" IN ACCESS EXCLUSIVE MODE
2025-10-17T03:56:45.472547167Z info: Microsoft.EntityFrameworkCore.Database.Command[20101]
2025-10-17T03:56:45.472611999Z       Executed DbCommand (66ms) [Parameters=[], CommandType='Text', CommandTimeout='60']
2025-10-17T03:56:45.472616519Z       SELECT "MigrationId", "ProductVersion"
2025-10-17T03:56:45.472618949Z       FROM "__EFMigrationsHistory"
2025-10-17T03:56:45.472630039Z       ORDER BY "MigrationId";
2025-10-17T03:56:45.476351969Z info: Microsoft.EntityFrameworkCore.Migrations[20405]
2025-10-17T03:56:45.476364529Z       No migrations were applied. The database is already up to date.
2025-10-17T03:56:45.547458297Z [DB] Migraciones aplicadas al arranque. Entorno: Production
2025-10-17T03:56:46.331729387Z warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
2025-10-17T03:56:46.331744047Z       Storing keys in a directory '/home/appuser/.aspnet/DataProtection-Keys' that may not be persisted outside of the container. Protected data will be unavailable when container is destroyed. For more information go to https://aka.ms/aspnet/dataprotectionwarning
2025-10-17T03:56:46.437566642Z warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
2025-10-17T03:56:46.437587982Z       No XML encryptor configured. Key {606b311e-b381-4b81-98bf-9c5bb0a06c23} may be persisted to storage in unencrypted form.
2025-10-17T03:56:46.534567327Z warn: Microsoft.AspNetCore.Hosting.Diagnostics[15]
2025-10-17T03:56:46.534587107Z       Overriding HTTP_PORTS '8080' and HTTPS_PORTS ''. Binding to values defined by URLS instead 'http://+:10000'.
2025-10-17T03:56:46.935061566Z info: Microsoft.Hosting.Lifetime[14]
2025-10-17T03:56:46.935079016Z       Now listening on: http://[::]:10000
2025-10-17T03:56:46.935088336Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:56:46.935092816Z       Application started. Press Ctrl+C to shut down.
2025-10-17T03:56:46.935127697Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:56:46.935133737Z       Hosting environment: Production
2025-10-17T03:56:46.935140487Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:56:46.935143527Z       Content root path: /app
2025-10-17T03:56:47.332332785Z warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
2025-10-17T03:56:47.332355866Z       Failed to determine the https port for redirect.
2025-10-17T03:56:47.441260677Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:56:47.441287218Z       REQUEST_START {"timestamp":"2025-10-17T03:56:47.333Z","level":"info","service":"Congreso.Api","trace_id":"23bdb680-9b02-412e-a239-79cb6fa99c35","request_id":"21898141-18d2-42f1-9372-ac94b599d19e","event_type":"request_start","http":{"method":"HEAD","endpoint":"/","user_agent":"Go-http-client/1.1","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T03:56:47.829036353Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:56:47.829055303Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:56:47.737Z","level":"warning","service":"Congreso.Api","trace_id":"23bdb680-9b02-412e-a239-79cb6fa99c35","request_id":"21898141-18d2-42f1-9372-ac94b599d19e","event_type":"request_complete","duration_ms":404,"http":{"method":"HEAD","endpoint":"/","status_code":404,"user_id":"anonymous","ip_address":"::1"}}
2025-10-17T03:56:50.519205712Z info: Microsoft.Hosting.Lifetime[0]
2025-10-17T03:56:50.519236503Z       Application is shutting down...
2025-10-17T03:56:50.606221846Z ==> Your service is live 🎉
2025-10-17T03:56:50.634426964Z ==> 
2025-10-17T03:56:50.659615823Z ==> ///////////////////////////////////////////////////////////
2025-10-17T03:56:50.682980102Z ==> 
2025-10-17T03:56:50.70854366Z ==> Available at your primary URL https://congreso-api.onrender.com
2025-10-17T03:56:50.733367788Z ==> 
2025-10-17T03:56:50.756994777Z ==> ///////////////////////////////////////////////////////////
2025-10-17T03:56:52.96290728Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:56:52.962932561Z       REQUEST_START {"timestamp":"2025-10-17T03:56:52.962Z","level":"info","service":"Congreso.Api","trace_id":"64dbf8e3-fa3c-4679-ac3e-bb307c33ab40","request_id":"302e201b-7393-4790-9863-c8ea9f5a051e","event_type":"request_start","http":{"method":"GET","endpoint":"/","user_agent":"Go-http-client/2.0","ip_address":"::1","user_id":"anonymous","query_string":"","referrer":null}}
2025-10-17T03:56:53.026239952Z info: Congreso.Api.Middleware.StructuredLoggingMiddleware[0]
2025-10-17T03:56:53.026409885Z       REQUEST_COMPLETE {"timestamp":"2025-10-17T03:56:52.966Z","level":"warning","service":"Congreso.Api","trace_id":"64dbf8e3-fa3c-4679-ac3e-bb307c33ab40","request_id":"302e201b-7393-4790-9863-c8ea9f5a051e","event_type":"request_complete","duration_ms":4,"http":{"method":"GET","endpoint":"/","status_code":404,"user_id":"anonymous","ip_address":"::1"}}