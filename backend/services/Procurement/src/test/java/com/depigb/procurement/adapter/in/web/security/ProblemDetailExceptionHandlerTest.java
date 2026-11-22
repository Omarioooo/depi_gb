package com.depigb.procurement.adapter.in.web.security;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.depigb.procurement.config.SecurityConfig;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;

@WebMvcTest(controllers = AuthorizationTestController.class)
@Import(SecurityConfig.class)
class ProblemDetailExceptionHandlerTest {

    private static final String USER_ID = "33333333-3333-3333-3333-333333333333";

    @Autowired
    private MockMvc mockMvc;

    @Test
    void validationFailureReturnsSafeProblemDetail() throws Exception {
        mockMvc.perform(post("/test/procurement/validate")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{}")
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isBadRequest())
                .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.code").value("VALIDATION_FAILED"))
                .andExpect(jsonPath("$.errors[0].field").value("name"));
    }

    @Test
    void unexpectedFailureReturnsGenericProblemDetail() throws Exception {
        MvcResult result = mockMvc.perform(get("/test/procurement/fail")
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isInternalServerError())
                .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_PROBLEM_JSON))
                .andExpect(jsonPath("$.status").value(500))
                .andExpect(jsonPath("$.code").value("INTERNAL_ERROR"))
                .andReturn();

        String body = result.getResponse().getContentAsString();
        assertThat(body)
                .doesNotContain("internal detail")
                .doesNotContain("IllegalStateException")
                .doesNotContain("com.depigb");
    }

    @Test
    void unknownPathReturnsNotFoundInsteadOfInternalError() throws Exception {
        mockMvc.perform(get("/test/procurement/unknown")
                        .header(GatewayHeaderFilter.USER_ID_HEADER, USER_ID)
                        .header(GatewayHeaderFilter.USER_ROLES_HEADER, "MANAGER"))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("NOT_FOUND"));
    }
}
