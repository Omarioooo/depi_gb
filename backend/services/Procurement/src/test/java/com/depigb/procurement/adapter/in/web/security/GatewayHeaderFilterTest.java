package com.depigb.procurement.adapter.in.web.security;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.header;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.depigb.procurement.adapter.in.web.ServiceStatusController;
import com.depigb.procurement.config.SecurityConfig;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.test.web.servlet.request.MockHttpServletRequestBuilder;

@WebMvcTest(controllers = {AuthorizationTestController.class, ServiceStatusController.class})
@Import(SecurityConfig.class)
class GatewayHeaderFilterTest {

    private static final String USER_ID = "11111111-1111-1111-1111-111111111111";
    private static final String PROTECTED_PATH = "/test/procurement/orders";

    @Autowired
    private MockMvc mockMvc;

    @Test
    void publicStatusEndpointDoesNotRequireIdentityHeaders() throws Exception {
        mockMvc.perform(get("/api/procurement/status"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("UP"));
    }

    @Test
    void publicEndpointStillPropagatesCorrelationId() throws Exception {
        mockMvc.perform(get("/api/procurement/status")
                        .header(GatewayHeaderFilter.CORRELATION_ID_HEADER, "status-correlation-1"))
                .andExpect(status().isOk())
                .andExpect(header().string(
                        GatewayHeaderFilter.CORRELATION_ID_HEADER, "status-correlation-1"));
    }

    @Test
    void readsGatewayIdentityAndCorrelationHeaders() throws Exception {
        mockMvc.perform(authorizedGet("MANAGER")
                        .header(GatewayHeaderFilter.CORRELATION_ID_HEADER, "test-correlation-1"))
                .andExpect(status().isOk())
                .andExpect(header().string(
                        GatewayHeaderFilter.CORRELATION_ID_HEADER, "test-correlation-1"))
                .andExpect(jsonPath("$.userId").value(USER_ID))
                .andExpect(jsonPath("$.roles[0]").value("MANAGER"))
                .andExpect(jsonPath("$.correlationId").value("test-correlation-1"))
                .andExpect(jsonPath("$.mdcCorrelationId").value("test-correlation-1"));
    }

    @Test
    void normalizesRolesCaseInsensitivelyAndIgnoresUnknownNames() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, " manager , auditor "))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.roles.length()").value(1))
                .andExpect(jsonPath("$.roles[0]").value("MANAGER"));
    }

    @Test
    void generatesCorrelationIdWhenGatewayHeaderIsMissing() throws Exception {
        MvcResult result = mockMvc.perform(authorizedGet("MANAGER"))
                .andExpect(status().isOk())
                .andReturn();

        String correlationId = result.getResponse()
                .getHeader(GatewayHeaderFilter.CORRELATION_ID_HEADER);

        assertThat(correlationId).isNotBlank();
        assertThat(UUID.fromString(correlationId)).isNotNull();
    }

    @Test
    void missingUserIdIsRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isForbidden())
                .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void blankUserIdIsRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, "  ")
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void missingRolesAreRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void unknownRolesOnlyAreRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "AUDITOR"))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void duplicateUserIdHeadersAreRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void duplicateRoleHeadersAreRejected() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER", "STAFF"))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void queryParametersCannotOverrideGatewayIdentity() throws Exception {
        mockMvc.perform(get(PROTECTED_PATH)
                        .queryParam("userId", "attacker")
                        .queryParam("roles", "SYSTEM_ADMIN")
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.userId").value(USER_ID))
                .andExpect(jsonPath("$.roles[0]").value("MANAGER"));
    }

    @Test
    void correlationIdIsClearedFromMdcAfterRequest() throws Exception {
        mockMvc.perform(authorizedGet("MANAGER")
                        .header(GatewayHeaderFilter.CORRELATION_ID_HEADER, "corr-cleanup"))
                .andExpect(status().isOk());

        assertThat(MDC.get(GatewayHeaderFilter.CORRELATION_ID_MDC_KEY)).isNull();
    }

    @Test
    void systemAdministratorOnlyIsRejectedAtTheBoundary() throws Exception {
        mockMvc.perform(authorizedGet("SYSTEM_ADMIN"))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.detail").value("Access denied."))
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"));
    }

    @Test
    void forbiddenResponseDoesNotLeakIdentityOrInternals() throws Exception {
        MvcResult result = mockMvc.perform(authorizedGet("SYSTEM_ADMIN"))
                .andExpect(status().isForbidden())
                .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.status").value(403))
                .andExpect(jsonPath("$.title").value("Forbidden"))
                .andExpect(jsonPath("$.code").value("ACCESS_DENIED"))
                .andReturn();

        String body = result.getResponse().getContentAsString();
        assertThat(body)
                .doesNotContain(USER_ID)
                .doesNotContain("SYSTEM_ADMIN")
                .doesNotContain("AuthorizationDeniedException")
                .doesNotContain("com.depigb")
                .doesNotContain("at com.");
    }

    private MockHttpServletRequestBuilder authorizedGet(String roles) {
        return get(PROTECTED_PATH)
                .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                .header(GatewayHeaderFilter.USER_ROLES_HEADER, roles);
    }
}
