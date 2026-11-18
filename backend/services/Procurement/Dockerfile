ARG JAVA_VERSION=21

# Build stage. The wrapper keeps the container build aligned with the Gradle
# version committed in this service.
FROM eclipse-temurin:${JAVA_VERSION}-jdk-jammy AS builder

WORKDIR /workspace

RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl unzip \
    && rm -rf /var/lib/apt/lists/*

COPY gradlew settings.gradle.kts build.gradle.kts ./
COPY gradle/ gradle/

RUN chmod +x gradlew

# Cache dependency resolution separately from application source changes.
RUN ./gradlew dependencies --no-daemon

COPY src/ src/

RUN ./gradlew clean bootJar --no-daemon -x test \
    && jar_path="$(find build/libs -maxdepth 1 -type f -name '*.jar' ! -name '*-plain.jar' | head -n 1)" \
    && test -n "$jar_path" \
    && cp "$jar_path" /workspace/app.jar

# Runtime stage. It contains only the JRE and the packaged application.
FROM eclipse-temurin:${JAVA_VERSION}-jre-jammy AS runtime

WORKDIR /app

RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system spring \
    && useradd --system --gid spring --home-dir /app --shell /usr/sbin/nologin spring

COPY --from=builder --chown=spring:spring /workspace/app.jar /app/app.jar

USER spring:spring

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/actuator/health || exit 1

ENTRYPOINT ["java", "-jar", "/app/app.jar"]
