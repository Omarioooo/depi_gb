package com.depigb.procurement.adapter.in.web.security;

import com.depigb.procurement.config.SecurityConfig;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Collections;
import java.util.EnumSet;
import java.util.Enumeration;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.regex.Pattern;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.util.AntPathMatcher;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Reads the trusted gateway identity headers and makes them available as a
 * request-scoped {@link UserContext}.
 *
 * <p>The service never authenticates users. It trusts the gateway to set the
 * headers, rejects missing or ambiguous identity, and protects every path that
 * is not explicitly public.</p>
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE + 10)
public class GatewayHeaderFilter extends OncePerRequestFilter {

    private static final Logger log = LoggerFactory.getLogger(GatewayHeaderFilter.class);

    public static final String USER_ID_HEADER = "X-User-Id";
    public static final String USER_ROLES_HEADER = "X-User-Roles";
    public static final String CORRELATION_ID_HEADER = "X-Correlation-Id";
    public static final String CORRELATION_ID_MDC_KEY = "correlationId";

    private static final String PROBLEM_TYPE = "https://depigb.example/problems/access-denied";
    private static final String ACCESS_DENIED_CODE = "ACCESS_DENIED";
    private static final String MISSING_CONTEXT_DETAIL = "Missing trusted user context.";
    private static final String AMBIGUOUS_CONTEXT_DETAIL = "Ambiguous trusted user context.";
    private static final String ACCESS_DENIED_DETAIL = "Access denied.";
    private static final Pattern SAFE_CORRELATION_ID =
            Pattern.compile("[A-Za-z0-9._:-]{1,128}");
    private static final AntPathMatcher PATH_MATCHER = new AntPathMatcher();

    private final ObjectMapper objectMapper;

    public GatewayHeaderFilter(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }

    @Override
    protected void doFilterInternal(
            HttpServletRequest request,
            HttpServletResponse response,
            FilterChain filterChain) throws ServletException, IOException {

        String correlationId = resolveCorrelationId(request);
        MDC.put(CORRELATION_ID_MDC_KEY, correlationId);
        try {
            response.setHeader(CORRELATION_ID_HEADER, correlationId);

            if (isPublicRequest(request)) {
                filterChain.doFilter(request, response);
                return;
            }

            authenticate(request, response, filterChain, correlationId);
        } finally {
            MDC.remove(CORRELATION_ID_MDC_KEY);
        }
    }

    private boolean isPublicRequest(HttpServletRequest request) {
        if ("OPTIONS".equalsIgnoreCase(request.getMethod())) {
            return true;
        }
        String path = request.getRequestURI();
        return SecurityConfig.PUBLIC_ENDPOINTS.stream()
                .anyMatch(pattern -> PATH_MATCHER.match(pattern, path));
    }

    private void authenticate(
            HttpServletRequest request,
            HttpServletResponse response,
            FilterChain filterChain,
            String correlationId) throws ServletException, IOException {

        List<String> userIdValues = headerValues(request, USER_ID_HEADER);
        if (userIdValues.size() > 1) {
            reject(request, response, AMBIGUOUS_CONTEXT_DETAIL);
            return;
        }
        if (userIdValues.isEmpty()) {
            reject(request, response, MISSING_CONTEXT_DETAIL);
            return;
        }
        String userId = userIdValues.getFirst().trim();
        if (userId.isEmpty() || userId.indexOf(',') >= 0) {
            reject(request, response, userId.isEmpty() ? MISSING_CONTEXT_DETAIL : AMBIGUOUS_CONTEXT_DETAIL);
            return;
        }

        List<String> roleValues = headerValues(request, USER_ROLES_HEADER);
        if (roleValues.size() > 1) {
            reject(request, response, AMBIGUOUS_CONTEXT_DETAIL);
            return;
        }
        if (roleValues.isEmpty() || roleValues.getFirst().isBlank()) {
            reject(request, response, MISSING_CONTEXT_DETAIL);
            return;
        }
        Set<Role> roles = Arrays.stream(roleValues.getFirst().split(","))
                .map(Role::from)
                .flatMap(Optional::stream)
                .collect(Collectors.toCollection(() -> EnumSet.noneOf(Role.class)));
        if (roles.isEmpty()) {
            reject(request, response, MISSING_CONTEXT_DETAIL);
            return;
        }
        if (roles.contains(Role.SYSTEM_ADMIN)
                && !roles.contains(Role.MANAGER)
                && !roles.contains(Role.STAFF)) {
            reject(request, response, ACCESS_DENIED_DETAIL);
            return;
        }

        UserContext context = new UserContext(userId, roles, correlationId);
        request.setAttribute(UserContext.REQUEST_ATTRIBUTE, context);
        filterChain.doFilter(request, response);
    }

    private List<String> headerValues(HttpServletRequest request, String headerName) {
        Enumeration<String> values = request.getHeaders(headerName);
        if (values == null) {
            return List.of();
        }
        return Collections.list(values);
    }

    private String resolveCorrelationId(HttpServletRequest request) {
        List<String> values = headerValues(request, CORRELATION_ID_HEADER);
        if (!values.isEmpty()) {
            String candidate = values.getFirst().trim();
            if (SAFE_CORRELATION_ID.matcher(candidate).matches()) {
                return candidate;
            }
        }
        return UUID.randomUUID().toString();
    }

    private void reject(HttpServletRequest request, HttpServletResponse response, String detail)
            throws IOException {
        log.warn("Rejected request to {}: {}", request.getRequestURI(), detail);
        response.setStatus(HttpStatus.FORBIDDEN.value());
        response.setContentType(MediaType.APPLICATION_PROBLEM_JSON_VALUE);
        response.setCharacterEncoding(StandardCharsets.UTF_8.name());

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("type", PROBLEM_TYPE);
        body.put("title", "Forbidden");
        body.put("status", HttpStatus.FORBIDDEN.value());
        body.put("detail", detail);
        body.put("instance", request.getRequestURI());
        body.put("code", ACCESS_DENIED_CODE);

        objectMapper.writeValue(response.getOutputStream(), body);
    }
}
