package com.depigb.procurement.adapter.in.web.security;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.depigb.procurement.adapter.in.web.security.AuthorizationRules.Operation;
import com.depigb.procurement.config.SecurityConfig;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.EnumSource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.context.annotation.Import;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockHttpServletRequestBuilder;

@WebMvcTest(controllers = AuthorizationTestController.class)
@Import(SecurityConfig.class)
class RoleMatrixTest {

    private static final String USER_ID = "22222222-2222-2222-2222-222222222222";

    @Autowired
    private MockMvc mockMvc;

    @ParameterizedTest
    @EnumSource(Operation.class)
    void managerCanPerformEveryProcurementOperation(Operation operation) throws Exception {
        mockMvc.perform(as(Role.MANAGER, operation))
                .andExpect(status().isOk());
    }

    @ParameterizedTest
    @EnumSource(value = Operation.class, names = {
            "READ_ORDERS", "READ_RECEIPTS", "SUBMIT_ORDER", "RECORD_RECEIPT", "RETRY_RECEIPT"})
    void staffCanPerformOperationalTasks(Operation operation) throws Exception {
        mockMvc.perform(as(Role.STAFF, operation))
                .andExpect(status().isOk());
    }

    @ParameterizedTest
    @EnumSource(value = Operation.class, names = {
            "CREATE_DRAFT", "EDIT_DRAFT", "APPROVE_ORDER", "CANCEL_ORDER"})
    void staffCannotPerformManagerialOperations(Operation operation) throws Exception {
        mockMvc.perform(as(Role.STAFF, operation))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @ParameterizedTest
    @EnumSource(Operation.class)
    void systemAdministratorIsDeniedEveryProcurementOperation(Operation operation)
            throws Exception {
        mockMvc.perform(as(Role.SYSTEM_ADMIN, operation))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void systemAdministratorWithStaffRoleGetsOnlyStaffAccess() throws Exception {
        mockMvc.perform(as("SYSTEM_ADMIN,STAFF", Operation.READ_ORDERS))
                .andExpect(status().isOk());

        mockMvc.perform(as("SYSTEM_ADMIN,STAFF", Operation.CREATE_DRAFT))
                .andExpect(status().isForbidden());
    }

    private MockHttpServletRequestBuilder as(Role role, Operation operation) {
        return as(role.name(), operation);
    }

    private MockHttpServletRequestBuilder as(String roles, Operation operation) {
        return requestFor(operation)
                .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                .header(GatewayHeaderFilter.USER_ROLES_HEADER, roles);
    }

    private MockHttpServletRequestBuilder requestFor(Operation operation) {
        return switch (operation) {
            case READ_ORDERS -> get("/test/procurement/orders");
            case READ_RECEIPTS -> get("/test/procurement/receipts");
            case CREATE_DRAFT -> post("/test/procurement/drafts");
            case EDIT_DRAFT -> put("/test/procurement/drafts/1");
            case SUBMIT_ORDER -> post("/test/procurement/orders/1/submit");
            case APPROVE_ORDER -> post("/test/procurement/orders/1/approve");
            case CANCEL_ORDER -> post("/test/procurement/orders/1/cancel");
            case RECORD_RECEIPT -> post("/test/procurement/orders/1/receipts");
            case RETRY_RECEIPT -> post("/test/procurement/receipts/1/retry");
        };
    }
}
