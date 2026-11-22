package com.depigb.procurement.config;

import com.depigb.procurement.adapter.in.web.security.AuthorizationRules;
import java.util.List;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Wires the trusted-gateway authorization boundary and declares the only
 * endpoints that are reachable without an identity context.
 */
@Configuration
public class SecurityConfig {

    /**
     * Endpoints that must stay reachable without gateway identity headers.
     * Everything else is protected by {@code GatewayHeaderFilter}.
     */
    public static final List<String> PUBLIC_ENDPOINTS = List.of(
            "/actuator/health",
            "/actuator/health/**",
            "/actuator/info",
            "/api/procurement/status");

    @Bean
    public AuthorizationRules authorizationRules() {
        return new AuthorizationRules();
    }
}
