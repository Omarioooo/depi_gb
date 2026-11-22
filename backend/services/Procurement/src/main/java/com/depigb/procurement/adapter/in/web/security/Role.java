package com.depigb.procurement.adapter.in.web.security;

import java.util.Locale;
import java.util.Optional;

/**
 * Roles trusted from the API gateway.
 *
 * <p>Authentication happens at the gateway. This enum only models the role
 * names the Procurement service understands; unknown names are ignored.</p>
 */
public enum Role {
    MANAGER,
    STAFF,
    SYSTEM_ADMIN;

    public static Optional<Role> from(String rawValue) {
        if (rawValue == null) {
            return Optional.empty();
        }
        String normalized = rawValue.trim().toUpperCase(Locale.ROOT);
        if (normalized.isEmpty()) {
            return Optional.empty();
        }
        try {
            return Optional.of(valueOf(normalized));
        } catch (IllegalArgumentException ex) {
            return Optional.empty();
        }
    }
}
