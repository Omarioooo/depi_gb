package com.depigb.procurement.adapter.in.web;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(ServiceStatusController.class)
class ServiceStatusControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @Test
    void returnsServiceStatus() throws Exception {
        mockMvc.perform(get("/api/procurement/status"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.service").value("procurement-service"))
                .andExpect(jsonPath("$.status").value("UP"))
                .andExpect(jsonPath("$.version").value("1.0-SNAPSHOT"))
                .andExpect(jsonPath("$.environment").value("local"));
    }
}
