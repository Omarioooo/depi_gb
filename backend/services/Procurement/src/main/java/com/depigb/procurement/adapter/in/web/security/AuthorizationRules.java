package com.depigb.procurement.adapter.in.web.security;

import java.util.EnumSet;
import java.util.Objects;
import java.util.Set;

/**
 * Single source of truth for the procurement role matrix.
 *
 * <p>Controllers and use cases call {@link #require(UserContext, Operation)}
 * before performing work. The matrix is intentionally explicit: a
 * {@code SYSTEM_ADMIN} gets no procurement business access by default.</p>
 */
public class AuthorizationRules {

    /** Operations that can be protected by the procurement role matrix. */
    public enum Operation {
        CREATE_DRAFT,
        EDIT_DRAFT,
        READ_ORDERS,
        READ_RECEIPTS,
        SUBMIT_ORDER,
        APPROVE_ORDER,
        CANCEL_ORDER,
        RECORD_RECEIPT,
        RETRY_RECEIPT
    }

    private static final Set<Operation> MANAGER_OPERATIONS =
            EnumSet.allOf(Operation.class);

    private static final Set<Operation> STAFF_OPERATIONS = EnumSet.of(
            Operation.READ_ORDERS,
            Operation.READ_RECEIPTS,
            Operation.SUBMIT_ORDER,
            Operation.RECORD_RECEIPT,
            Operation.RETRY_RECEIPT);

    public boolean isAllowed(UserContext context, Operation operation) {
        Objects.requireNonNull(context, "context");
        Objects.requireNonNull(operation, "operation");

        if (context.hasRole(Role.MANAGER) && MANAGER_OPERATIONS.contains(operation)) {
            return true;
        }
        return context.hasRole(Role.STAFF) && STAFF_OPERATIONS.contains(operation);
    }

    public void require(UserContext context, Operation operation) {
        if (!isAllowed(context, operation)) {
            throw new AuthorizationDeniedException();
        }
    }
}
