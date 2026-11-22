package com.depigb.procurement.adapter.in.web.security;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatCode;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.depigb.procurement.adapter.in.web.security.AuthorizationRules.Operation;
import java.util.EnumSet;
import java.util.Set;
import org.junit.jupiter.api.Test;

class AuthorizationRulesTest {

    private final AuthorizationRules rules = new AuthorizationRules();

    @Test
    void managerCanPerformEveryOperation() {
        UserContext manager = context(Role.MANAGER);

        for (Operation operation : Operation.values()) {
            assertThat(rules.isAllowed(manager, operation))
                    .as("manager should be allowed to %s", operation)
                    .isTrue();
        }
    }

    @Test
    void staffCanPerformOnlyOperationalTasks() {
        UserContext staff = context(Role.STAFF);
        Set<Operation> allowed = EnumSet.of(
                Operation.READ_ORDERS,
                Operation.READ_RECEIPTS,
                Operation.SUBMIT_ORDER,
                Operation.RECORD_RECEIPT,
                Operation.RETRY_RECEIPT);

        for (Operation operation : Operation.values()) {
            assertThat(rules.isAllowed(staff, operation))
                    .as("staff access to %s", operation)
                    .isEqualTo(allowed.contains(operation));
        }
    }

    @Test
    void systemAdministratorIsDeniedEveryOperation() {
        UserContext systemAdmin = context(Role.SYSTEM_ADMIN);

        for (Operation operation : Operation.values()) {
            assertThat(rules.isAllowed(systemAdmin, operation))
                    .as("system administrator access to %s", operation)
                    .isFalse();
        }
    }

    @Test
    void systemAdministratorWithStaffRoleGetsOnlyStaffAccess() {
        UserContext combined = context(Role.SYSTEM_ADMIN, Role.STAFF);

        assertThat(rules.isAllowed(combined, Operation.READ_ORDERS)).isTrue();
        assertThat(rules.isAllowed(combined, Operation.CREATE_DRAFT)).isFalse();
    }

    @Test
    void userWithoutKnownRolesIsDenied() {
        UserContext unknown = new UserContext("user-1", Set.of(), "corr-1");

        for (Operation operation : Operation.values()) {
            assertThat(rules.isAllowed(unknown, operation)).isFalse();
        }
    }

    @Test
    void requireThrowsWhenOperationIsDenied() {
        assertThatThrownBy(() -> rules.require(context(Role.STAFF), Operation.APPROVE_ORDER))
                .isInstanceOf(AuthorizationDeniedException.class);
    }

    @Test
    void requireAllowsWhenOperationIsPermitted() {
        assertThatCode(() -> rules.require(context(Role.STAFF), Operation.SUBMIT_ORDER))
                .doesNotThrowAnyException();
    }

    private UserContext context(Role... roles) {
        return new UserContext("user-1", Set.of(roles), "corr-1");
    }
}
