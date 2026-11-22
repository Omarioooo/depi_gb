package com.depigb.procurement.adapter.in.web.security;

import java.util.Objects;
import java.util.Set;

/**
 * Request-scoped identity supplied by the trusted API gateway.
 *
 * <p>This type is deliberately web-scoped: the application and domain layers
 * receive the resolved identity only when a use case needs it.</p>
 */
public record UserContext(String userId, Set<Role> roles, String correlationId) {

    /**
     * Request attribute name used by the gateway filter and controllers.
     * Kept as a literal so it can be referenced from annotations.
     */
    public static final String REQUEST_ATTRIBUTE =
            "com.depigb.procurement.adapter.in.web.security.UserContext";

    public UserContext {
        Objects.requireNonNull(userId, "userId");
        Objects.requireNonNull(correlationId, "correlationId");
        roles = Set.copyOf(Objects.requireNonNull(roles, "roles"));
    }

    public boolean hasRole(Role role) {
        return roles.contains(role);
    }
}
