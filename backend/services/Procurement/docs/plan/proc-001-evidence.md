# PROC-001 — Closeout Evidence

Date: 2026-11-21

Host port `8080` was already owned by `coolify-proxy`, so the container used host
port `18080`. The application still listened on container port `8080`.

## Gradle build

```bash
./gradlew clean build --console=plain
```

```text
> Task :clean
> Task :compileJava
> Task :processResources
> Task :classes
> Task :resolveMainClassName
> Task :bootJar
> Task :jar
> Task :assemble
> Task :compileTestJava
> Task :processTestResources NO-SOURCE
> Task :testClasses
OpenJDK 64-Bit Server VM warning: Sharing is only supported for boot loader classes because bootstrap classpath has been appended
2026-11-21T16:36:11.567+02:00  INFO 2545288 --- [procurement-service] [ionShutdownHook] o.s.b.w.e.tomcat.GracefulShutdown        : Commencing graceful shutdown. Waiting for active requests to complete
2026-11-21T16:36:11.573+02:00  INFO 2545288 --- [procurement-service] [tomcat-shutdown] o.s.b.w.e.tomcat.GracefulShutdown        : Graceful shutdown complete
> Task :test
> Task :check
> Task :build

BUILD SUCCESSFUL in 26s
8 actionable tasks: 8 executed
Consider enabling configuration cache to speed up this build: https://docs.gradle.org/9.6.0/userguide/configuration_cache_enabling.html
```

## Docker image build

```bash
docker build -t procurement-service:local .
```

```text
DEPRECATED: The legacy builder is deprecated and will be removed in a future release.
            Install the buildx component to build images with BuildKit:
            https://docs.docker.com/go/buildx/

Sending build context to Docker daemon  60.92MB
Step 1/18 : ARG JAVA_VERSION=21
Step 2/18 : FROM eclipse-temurin:${JAVA_VERSION}-jdk-jammy AS builder
 ---> b2711c65970c
Step 3/18 : WORKDIR /workspace
 ---> Using cache
 ---> ae3b589b5c08
Step 4/18 : RUN apt-get update     && apt-get install --no-install-recommends --yes curl unzip     && rm -rf /var/lib/apt/lists/*
 ---> Using cache
 ---> 16ad0967966a
Step 5/18 : COPY gradlew settings.gradle.kts build.gradle.kts ./
 ---> Using cache
 ---> 4427a83572fa
Step 6/18 : COPY gradle/ gradle/
 ---> Using cache
 ---> 9bb16acf8a70
Step 7/18 : RUN chmod +x gradlew
 ---> Using cache
 ---> 03500db8bf5c
Step 8/18 : RUN ./gradlew dependencies --no-daemon
 ---> Using cache
 ---> 2fd180dc71a6
Step 9/18 : COPY src/ src/
 ---> Using cache
 ---> 951bc7e8983c
Step 10/18 : RUN ./gradlew clean bootJar --no-daemon -x test     && jar_path="$(find build/libs -maxdepth 1 -type f -name '*.jar' ! -name '*-plain.jar' | head -n 1)"     && test -n "$jar_path"     && cp "$jar_path" /workspace/app.jar
 ---> Using cache
 ---> ea9336384626
Step 11/18 : FROM eclipse-temurin:${JAVA_VERSION}-jre-jammy AS runtime
 ---> a8bff88f760e
Step 12/18 : WORKDIR /app
 ---> Using cache
 ---> 5ceedd0c904d
Step 13/18 : RUN apt-get update     && apt-get install --no-install-recommends --yes curl     && rm -rf /var/lib/apt/lists/*     && groupadd --system spring     && useradd --system --gid spring --home-dir /app --shell /usr/sbin/nologin spring
 ---> Using cache
 ---> 22fbef2572fa
Step 14/18 : COPY --from=builder --chown=spring:spring /workspace/app.jar /app/app.jar
 ---> Using cache
 ---> 35248478295d
Step 15/18 : USER spring:spring
 ---> Using cache
 ---> 7fd7ae14ce47
Step 16/18 : EXPOSE 8080
 ---> Using cache
 ---> 066317137ad3
Step 17/18 : HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3     CMD curl --fail --silent http://127.0.0.1:8080/actuator/health || exit 1
 ---> Using cache
 ---> 839b1373b0d8
Step 18/18 : ENTRYPOINT ["java", "-jar", "/app/app.jar"]
 ---> Using cache
 ---> cfa27aaf0621
Successfully built cfa27aaf0621
Successfully tagged procurement-service:local
```

## Application startup

```bash
docker run --rm -d --name procurement-service -p 18080:8080 procurement-service:local
docker logs procurement-service
```

```text
2026-11-21T14:34:50.025Z  INFO 1 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : Starting ProcurementApplication v1.0-SNAPSHOT using Java 21.0.13.1 with PID 1 (/app/app.jar started by spring in /app)
2026-11-21T14:34:50.041Z  INFO 1 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : No active profile set, falling back to 1 default profile: "default"
2026-11-21T14:34:54.940Z  INFO 1 --- [procurement-service] [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat initialized with port 8080 (http)
2026-11-21T14:34:54.976Z  INFO 1 --- [procurement-service] [           main] o.apache.catalina.core.StandardService   : Starting service [Tomcat]
2026-11-21T14:34:54.977Z  INFO 1 --- [procurement-service] [           main] o.apache.catalina.core.StandardEngine    : Starting Servlet engine: [Apache Tomcat/10.1.46]
2026-11-21T14:34:55.044Z  INFO 1 --- [procurement-service] [           main] o.a.c.c.C.[Tomcat].[localhost].[/]       : Initializing Spring embedded WebApplicationContext
2026-11-21T14:34:55.048Z  INFO 1 --- [procurement-service] [           main] w.s.c.ServletWebServerApplicationContext : Root WebApplicationContext: initialization completed in 4826 ms
2026-11-21T14:34:57.933Z  INFO 1 --- [procurement-service] [           main] o.s.b.a.e.web.EndpointLinksResolver      : Exposing 2 endpoints beneath base path '/actuator'
2026-11-21T14:34:58.316Z  INFO 1 --- [procurement-service] [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat started on port 8080 (http) with context path '/'
2026-11-21T14:34:58.386Z  INFO 1 --- [procurement-service] [           main] c.d.procurement.ProcurementApplication   : Started ProcurementApplication in 9.991 seconds (process running for 11.713)
```

## Health

```bash
curl --fail --show-error --silent --write-out '\nHTTP %{http_code}\n' http://127.0.0.1:18080/actuator/health
```

```text
{"status":"UP","groups":["liveness","readiness"]}
HTTP 200
```

## Procurement status

```bash
curl --fail --show-error --silent --write-out '\nHTTP %{http_code}\n' http://127.0.0.1:18080/api/procurement/status
```

```text
{"service":"procurement-service","status":"UP","version":"1.0-SNAPSHOT","environment":"local"}
HTTP 200
```

The temporary container was stopped with `docker stop procurement-service`.
