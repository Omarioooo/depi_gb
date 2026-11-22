package com.depigb.procurement.adapter.in.web.security;

/**
 * Raised when the gateway identity is valid but the role matrix denies the
 * requested procurement operation.
 *
 * <p>The exception intentionally carries no message so it cannot leak identity
 * or internal details into an error response.</p>
 */
public class AuthorizationDeniedException extends RuntimeException {
}
