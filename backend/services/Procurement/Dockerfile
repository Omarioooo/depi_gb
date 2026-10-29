ARG JAVA_VERSION=21
# Stage 1: Build the application
FROM eclipse-temurin:${JAVA_VERSION}-jdk AS builder

WORKDIR /app

COPY gradle/ gradle/
COPY gradlew build.gradle settings.gradle ./

RUN chmod +x gradlew
RUN ./gradlew dependencies --no-daemon || true
COPY src/ src/
RUN ./gradlew bootJar --no-daemon -x test

# Stage 2: Create the runtime image
FROM eclipse-temurin:${JAVA_VERSION}-jre

WORKDIR /app

# Create a non-root user and group for security
RUN groupadd -r spring && useradd -r -g spring spring

# Copy the built jar from the builder stage
COPY --from=builder --chown=spring:spring /app/build/libs/app.jar app.jar

USER spring:spring
EXPOSE 8080

# Add a healthcheck leveraging Spring Boot Actuator
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD wget -q -O - http://127.0.0.1:8080/actuator/health || exit 1

# Run the application
ENTRYPOINT ["sh", "-c", "exec java -XX:MaxRAMPercentage=75 $JAVA_OPTS -jar app.jar"]